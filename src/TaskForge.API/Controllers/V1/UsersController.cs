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
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get all users with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _userService.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(result));
    }

    /// <summary>
    /// Get a user by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound(ApiResponse<UserDto>.Fail("User not found"));
        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    /// <summary>
    /// Create a new user (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 201)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var user = await _userService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, ApiResponse<UserDto>.Ok(user, "User created"));
    }

    /// <summary>
    /// Update a user.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userService.UpdateAsync(id, request);
        return Ok(ApiResponse<UserDto>.Ok(user, "User updated"));
    }

    /// <summary>
    /// Delete a user (Admin only).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Delete(int id)
    {
        await _userService.DeleteAsync(id);
        return Ok(ApiResponse<string>.Ok("Deleted", "User deleted successfully"));
    }

    /// <summary>
    /// Assign a role to a user (Admin only).
    /// </summary>
    [HttpPost("{userId}/roles/{roleId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> AssignRole(int userId, int roleId)
    {
        await _userService.AssignRoleAsync(userId, roleId);
        return Ok(ApiResponse<string>.Ok("Assigned", "Role assigned successfully"));
    }

    /// <summary>
    /// Remove a role from a user (Admin only).
    /// </summary>
    [HttpDelete("{userId}/roles/{roleId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> RemoveRole(int userId, int roleId)
    {
        await _userService.RemoveRoleAsync(userId, roleId);
        return Ok(ApiResponse<string>.Ok("Removed", "Role removed successfully"));
    }
}
