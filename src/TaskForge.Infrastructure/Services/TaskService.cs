using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskForge.Application.DTOs;
using TaskForge.Application.Interfaces;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Enums;
using TaskForge.Domain.Interfaces;

namespace TaskForge.Infrastructure.Services;

/// <summary>
/// Task management service with CRUD, filtering, pagination, and parallel bulk operations.
/// </summary>
public class TaskService : ITaskService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<TaskService> _logger;
    // SemaphoreSlim for thread-safe bulk operations
    private static readonly SemaphoreSlim _bulkOperationLock = new(1, 1);

    public TaskService(IUnitOfWork unitOfWork, ICacheService cache, ILogger<TaskService> logger)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<TaskItemDto?> GetByIdAsync(int id)
    {
        var cacheKey = $"task_{id}";
        var cached = _cache.Get<TaskItemDto>(cacheKey);
        if (cached != null) return cached;

        var task = await _unitOfWork.Repository<TaskItem>()
            .Query()
            .Include(t => t.Project)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null) return null;

        var dto = MapToDto(task);
        _cache.Set(cacheKey, dto);
        return dto;
    }

    public async Task<PagedResult<TaskItemDto>> GetAllAsync(int page = 1, int pageSize = 10, int? projectId = null, string? status = null)
    {
        var query = _unitOfWork.Repository<TaskItem>().Query()
            .Include(t => t.Project)
            .Include(t => t.AssignedTo)
            .AsQueryable();

        // Apply filters
        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TaskItemStatus>(status, out var statusEnum))
            query = query.Where(t => t.Status == statusEnum);

        var totalCount = await query.CountAsync();
        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<TaskItemDto>
        {
            Items = tasks.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<TaskItemDto> CreateAsync(CreateTaskRequest request)
    {
        // Validate project exists
        var project = await _unitOfWork.Repository<Project>().GetByIdAsync(request.ProjectId)
            ?? throw new KeyNotFoundException($"Project with ID {request.ProjectId} not found.");

        if (!Enum.TryParse<TaskPriority>(request.Priority, out var priority))
            priority = TaskPriority.Medium;

        var taskItem = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            Priority = priority,
            Status = TaskItemStatus.Pending,
            DueDate = request.DueDate,
            ProjectId = request.ProjectId,
            AssignedToId = request.AssignedToId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<TaskItem>().AddAsync(taskItem);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Task '{Title}' created in project {ProjectId}", taskItem.Title, taskItem.ProjectId);
        return await GetByIdAsync(taskItem.Id) ?? MapToDto(taskItem);
    }

    public async Task<TaskItemDto> UpdateAsync(int id, UpdateTaskRequest request)
    {
        var task = await _unitOfWork.Repository<TaskItem>().GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Task with ID {id} not found.");

        if (request.Title != null) task.Title = request.Title;
        if (request.Description != null) task.Description = request.Description;
        if (request.Status != null && Enum.TryParse<TaskItemStatus>(request.Status, out var status))
        {
            task.Status = status;
            if (status == TaskItemStatus.Completed)
                task.CompletedAt = DateTime.UtcNow;
        }
        if (request.Priority != null && Enum.TryParse<TaskPriority>(request.Priority, out var priority))
            task.Priority = priority;
        if (request.DueDate.HasValue) task.DueDate = request.DueDate.Value;
        if (request.AssignedToId.HasValue) task.AssignedToId = request.AssignedToId.Value;

        task.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<TaskItem>().UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync();
        _cache.Remove($"task_{id}");
        _logger.LogInformation("Task {Id} updated", id);

        return await GetByIdAsync(id) ?? MapToDto(task);
    }

    public async Task DeleteAsync(int id)
    {
        var task = await _unitOfWork.Repository<TaskItem>().GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Task with ID {id} not found.");

        await _unitOfWork.Repository<TaskItem>().DeleteAsync(task);
        await _unitOfWork.SaveChangesAsync();
        _cache.Remove($"task_{id}");
        _logger.LogInformation("Task {Id} deleted", id);
    }

    /// <summary>
    /// Bulk update task statuses using parallel processing with thread-safe locking.
    /// Demonstrates: Parallel.ForEachAsync, SemaphoreSlim, async/await best practices.
    /// </summary>
    public async Task<IEnumerable<TaskItemDto>> BulkUpdateStatusAsync(BulkUpdateTaskStatusRequest request)
    {
        if (!Enum.TryParse<TaskItemStatus>(request.Status, out var statusEnum))
            throw new ArgumentException($"Invalid status: {request.Status}");

        await _bulkOperationLock.WaitAsync();
        try
        {
            _logger.LogInformation("Starting bulk status update for {Count} tasks", request.TaskIds.Length);

            var tasks = await _unitOfWork.Repository<TaskItem>().Query()
                .Where(t => request.TaskIds.Contains(t.Id))
                .ToListAsync();

            // Parallel processing of status updates
            await Parallel.ForEachAsync(tasks, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (task, ct) =>
            {
                task.Status = statusEnum;
                task.UpdatedAt = DateTime.UtcNow;
                if (statusEnum == TaskItemStatus.Completed)
                    task.CompletedAt = DateTime.UtcNow;

                _cache.Remove($"task_{task.Id}");

                await Task.CompletedTask; // Entity state is tracked by EF Core
            });

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Bulk update completed for {Count} tasks", tasks.Count);

            return tasks.Select(MapToDto);
        }
        finally
        {
            _bulkOperationLock.Release();
        }
    }

    public async Task<IEnumerable<TaskItemDto>> GetByUserAsync(int userId)
    {
        var tasks = await _unitOfWork.Repository<TaskItem>().Query()
            .Include(t => t.Project)
            .Include(t => t.AssignedTo)
            .Where(t => t.AssignedToId == userId)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ToListAsync();

        return tasks.Select(MapToDto);
    }

    private static TaskItemDto MapToDto(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        Status = task.Status.ToString(),
        Priority = task.Priority.ToString(),
        DueDate = task.DueDate,
        ProjectId = task.ProjectId,
        ProjectName = task.Project?.Name ?? "",
        AssignedToId = task.AssignedToId,
        AssignedToName = task.AssignedTo != null ? $"{task.AssignedTo.FirstName} {task.AssignedTo.LastName}" : null,
        CreatedAt = task.CreatedAt,
        CompletedAt = task.CompletedAt
    };
}
