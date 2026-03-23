using System.Security.Claims;
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
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>
    /// Get all projects with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProjectDto>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _projectService.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PagedResult<ProjectDto>>.Ok(result));
    }

    /// <summary>
    /// Get a project by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null) return NotFound(ApiResponse<ProjectDto>.Fail("Project not found"));
        return Ok(ApiResponse<ProjectDto>.Ok(project));
    }

    /// <summary>
    /// Create a new project. Owner is set to the authenticated user.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), 201)]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var project = await _projectService.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, ApiResponse<ProjectDto>.Ok(project, "Project created"));
    }

    /// <summary>
    /// Update a project.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), 200)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectRequest request)
    {
        var project = await _projectService.UpdateAsync(id, request);
        return Ok(ApiResponse<ProjectDto>.Ok(project, "Project updated"));
    }

    /// <summary>
    /// Delete a project (Admin only).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Delete(int id)
    {
        await _projectService.DeleteAsync(id);
        return Ok(ApiResponse<string>.Ok("Deleted", "Project deleted successfully"));
    }
}
