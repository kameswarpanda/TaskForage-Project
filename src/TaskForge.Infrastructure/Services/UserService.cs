using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskForge.Application.DTOs;
using TaskForge.Application.Interfaces;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Interfaces;

namespace TaskForge.Infrastructure.Services;

/// <summary>
/// User management service with CRUD operations, pagination, and role assignment.
/// </summary>
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork unitOfWork, ICacheService cache, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var cacheKey = $"user_{id}";
        var cached = _cache.Get<UserDto>(cacheKey);
        if (cached != null) return cached;

        var user = await _unitOfWork.Repository<User>()
            .Query()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return null;

        var dto = MapToDto(user);
        _cache.Set(cacheKey, dto);
        return dto;
    }

    public async Task<PagedResult<UserDto>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        var query = _unitOfWork.Repository<User>().Query()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<UserDto>
        {
            Items = users.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        var exists = await _unitOfWork.Repository<User>().Query()
            .AnyAsync(u => u.Username == request.Username || u.Email == request.Email);

        if (exists)
            throw new InvalidOperationException("Username or email already exists.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Status = Domain.Enums.UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<User>().AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Assign roles if provided
        if (request.RoleIds?.Any() == true)
        {
            foreach (var roleId in request.RoleIds)
            {
                await _unitOfWork.Repository<UserRole>().AddAsync(new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    AssignedAt = DateTime.UtcNow
                });
            }
            await _unitOfWork.SaveChangesAsync();
        }

        _logger.LogInformation("User {Username} created", user.Username);
        return await GetByIdAsync(user.Id) ?? MapToDto(user);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"User with ID {id} not found.");

        if (request.Email != null) user.Email = request.Email;
        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.Status != null && Enum.TryParse<Domain.Enums.UserStatus>(request.Status, out var status))
            user.Status = status;

        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<User>().UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _cache.Remove($"user_{id}");
        _logger.LogInformation("User {Id} updated", id);

        return await GetByIdAsync(id) ?? MapToDto(user);
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"User with ID {id} not found.");

        await _unitOfWork.Repository<User>().DeleteAsync(user);
        await _unitOfWork.SaveChangesAsync();
        _cache.Remove($"user_{id}");
        _logger.LogInformation("User {Id} deleted", id);
    }

    public async Task AssignRoleAsync(int userId, int roleId)
    {
        var exists = await _unitOfWork.Repository<UserRole>().Query()
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (exists)
            throw new InvalidOperationException("User already has this role.");

        await _unitOfWork.Repository<UserRole>().AddAsync(new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow
        });
        await _unitOfWork.SaveChangesAsync();
        _cache.Remove($"user_{userId}");
    }

    public async Task RemoveRoleAsync(int userId, int roleId)
    {
        var userRole = await _unitOfWork.Repository<UserRole>().Query()
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId)
            ?? throw new KeyNotFoundException("Role assignment not found.");

        await _unitOfWork.Repository<UserRole>().DeleteAsync(userRole);
        await _unitOfWork.SaveChangesAsync();
        _cache.Remove($"user_{userId}");
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Status = user.Status.ToString(),
        CreatedAt = user.CreatedAt,
        Roles = user.UserRoles?.Select(ur => ur.Role.Name) ?? Enumerable.Empty<string>()
    };
}
