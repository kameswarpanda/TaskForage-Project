using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskForge.Application.DTOs;
using TaskForge.Application.Interfaces;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Interfaces;

namespace TaskForge.Infrastructure.Services;

/// <summary>
/// Project management service with CRUD and pagination.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(IUnitOfWork unitOfWork, ICacheService cache, ILogger<ProjectService> logger)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ProjectDto?> GetByIdAsync(int id)
    {
        var cacheKey = $"project_{id}";
        var cached = _cache.Get<ProjectDto>(cacheKey);
        if (cached != null) return cached;

        var project = await _unitOfWork.Repository<Project>()
            .Query()
            .Include(p => p.Owner)
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null) return null;

        var dto = MapToDto(project);
        _cache.Set(cacheKey, dto);
        return dto;
    }

    public async Task<PagedResult<ProjectDto>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        var query = _unitOfWork.Repository<Project>().Query()
            .Include(p => p.Owner)
            .Include(p => p.Tasks);

        var totalCount = await query.CountAsync();
        var projects = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ProjectDto>
        {
            Items = projects.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<ProjectDto> CreateAsync(int ownerId, CreateProjectRequest request)
    {
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _unitOfWork.Repository<Project>().AddAsync(project);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Project {Name} created by user {OwnerId}", project.Name, ownerId);

        return await GetByIdAsync(project.Id) ?? MapToDto(project);
    }

    public async Task<ProjectDto> UpdateAsync(int id, UpdateProjectRequest request)
    {
        var project = await _unitOfWork.Repository<Project>().GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Project with ID {id} not found.");

        if (request.Name != null) project.Name = request.Name;
        if (request.Description != null) project.Description = request.Description;
        if (request.IsActive.HasValue) project.IsActive = request.IsActive.Value;
        project.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Project>().UpdateAsync(project);
        await _unitOfWork.SaveChangesAsync();
        _cache.Remove($"project_{id}");
        _logger.LogInformation("Project {Id} updated", id);

        return await GetByIdAsync(id) ?? MapToDto(project);
    }

    public async Task DeleteAsync(int id)
    {
        var project = await _unitOfWork.Repository<Project>().GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Project with ID {id} not found.");

        await _unitOfWork.Repository<Project>().DeleteAsync(project);
        await _unitOfWork.SaveChangesAsync();
        _cache.Remove($"project_{id}");
        _logger.LogInformation("Project {Id} deleted", id);
    }

    private static ProjectDto MapToDto(Project project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        Description = project.Description,
        OwnerId = project.OwnerId,
        OwnerName = project.Owner != null ? $"{project.Owner.FirstName} {project.Owner.LastName}" : "",
        IsActive = project.IsActive,
        CreatedAt = project.CreatedAt,
        TaskCount = project.Tasks?.Count ?? 0
    };
}
