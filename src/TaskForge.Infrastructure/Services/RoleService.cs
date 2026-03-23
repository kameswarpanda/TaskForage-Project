using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskForge.Application.DTOs;
using TaskForge.Application.Interfaces;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Interfaces;

namespace TaskForge.Infrastructure.Services;

/// <summary>
/// Role management service.
/// </summary>
public class RoleService : IRoleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoleService> _logger;

    public RoleService(IUnitOfWork unitOfWork, ILogger<RoleService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<RoleDto?> GetByIdAsync(int id)
    {
        var role = await _unitOfWork.Repository<Role>().GetByIdAsync(id);
        return role == null ? null : new RoleDto { Id = role.Id, Name = role.Name, Description = role.Description };
    }

    public async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        var roles = await _unitOfWork.Repository<Role>().GetAllAsync();
        return roles.Select(r => new RoleDto { Id = r.Id, Name = r.Name, Description = r.Description });
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request)
    {
        var exists = await _unitOfWork.Repository<Role>().Query()
            .AnyAsync(r => r.Name == request.Name);

        if (exists)
            throw new InvalidOperationException("Role already exists.");

        var role = new Role { Name = request.Name, Description = request.Description, CreatedAt = DateTime.UtcNow };
        await _unitOfWork.Repository<Role>().AddAsync(role);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Role {Name} created", role.Name);
        return new RoleDto { Id = role.Id, Name = role.Name, Description = role.Description };
    }

    public async Task DeleteAsync(int id)
    {
        var role = await _unitOfWork.Repository<Role>().GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Role with ID {id} not found.");

        await _unitOfWork.Repository<Role>().DeleteAsync(role);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Role {Id} deleted", id);
    }
}
