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
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>
    /// Get all roles.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<RoleDto>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _roleService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<RoleDto>>.Ok(roles));
    }

    /// <summary>
    /// Get a role by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var role = await _roleService.GetByIdAsync(id);
        if (role == null) return NotFound(ApiResponse<RoleDto>.Fail("Role not found"));
        return Ok(ApiResponse<RoleDto>.Ok(role));
    }

    /// <summary>
    /// Create a new role (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), 201)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request)
    {
        var role = await _roleService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = role.Id }, ApiResponse<RoleDto>.Ok(role, "Role created"));
    }

    /// <summary>
    /// Delete a role (Admin only).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Delete(int id)
    {
        await _roleService.DeleteAsync(id);
        return Ok(ApiResponse<string>.Ok("Deleted", "Role deleted successfully"));
    }
}
