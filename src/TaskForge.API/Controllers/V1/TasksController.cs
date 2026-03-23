using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskForge.Application.DTOs;
using TaskForge.Application.Interfaces;

namespace TaskForge.API.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Get all tasks with optional filtering by project and status. Supports pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TaskItemDto>>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? projectId = null,
        [FromQuery] string? status = null)
    {
        var result = await _taskService.GetAllAsync(page, pageSize, projectId, status);
        return Ok(ApiResponse<PagedResult<TaskItemDto>>.Ok(result));
    }

    /// <summary>
    /// Get a task by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TaskItemDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var task = await _taskService.GetByIdAsync(id);
        if (task == null) return NotFound(ApiResponse<TaskItemDto>.Fail("Task not found"));
        return Ok(ApiResponse<TaskItemDto>.Ok(task));
    }

    /// <summary>
    /// Get all tasks assigned to a specific user.
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TaskItemDto>>), 200)]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var tasks = await _taskService.GetByUserAsync(userId);
        return Ok(ApiResponse<IEnumerable<TaskItemDto>>.Ok(tasks));
    }

    /// <summary>
    /// Create a new task.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaskItemDto>), 201)]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
    {
        var task = await _taskService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, ApiResponse<TaskItemDto>.Ok(task, "Task created"));
    }

    /// <summary>
    /// Update a task.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TaskItemDto>), 200)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskRequest request)
    {
        var task = await _taskService.UpdateAsync(id, request);
        return Ok(ApiResponse<TaskItemDto>.Ok(task, "Task updated"));
    }

    /// <summary>
    /// Delete a task (Admin only).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Delete(int id)
    {
        await _taskService.DeleteAsync(id);
        return Ok(ApiResponse<string>.Ok("Deleted", "Task deleted successfully"));
    }

    /// <summary>
    /// Bulk update task statuses using parallel processing.
    /// </summary>
    [HttpPatch("bulk-update-status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TaskItemDto>>), 200)]
    public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkUpdateTaskStatusRequest request)
    {
        var tasks = await _taskService.BulkUpdateStatusAsync(request);
        return Ok(ApiResponse<IEnumerable<TaskItemDto>>.Ok(tasks, "Bulk update completed"));
    }
}
