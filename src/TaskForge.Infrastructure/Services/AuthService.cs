using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskForge.Application.DTOs;
using TaskForge.Application.Interfaces;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Interfaces;
using TaskForge.Infrastructure.Auth;

namespace TaskForge.Infrastructure.Services;

/// <summary>
/// Authentication service handling JWT login, registration, refresh tokens, and Basic Auth validation.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtTokenProvider _tokenProvider;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUnitOfWork unitOfWork, JwtTokenProvider tokenProvider, ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = (await _unitOfWork.Repository<User>()
            .Query()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username))
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (user.Status != Domain.Enums.UserStatus.Active)
            throw new UnauthorizedAccessException("Account is not active.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _tokenProvider.GenerateAccessToken(user, roles);
        var refreshToken = JwtTokenProvider.GenerateRefreshToken();

        // Save refresh token
        var tokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<RefreshToken>().AddAsync(tokenEntity);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {Username} logged in successfully", user.Username);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = MapToUserDto(user, roles)
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check existing user
        var exists = await _unitOfWork.Repository<User>()
            .Query()
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

        // Assign default "User" role
        var userRole = new UserRole { UserId = user.Id, RoleId = 2, AssignedAt = DateTime.UtcNow };
        await _unitOfWork.Repository<UserRole>().AddAsync(userRole);
        await _unitOfWork.SaveChangesAsync();

        var roles = new List<string> { "User" };
        var accessToken = _tokenProvider.GenerateAccessToken(user, roles);
        var refreshToken = JwtTokenProvider.GenerateRefreshToken();

        var tokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<RefreshToken>().AddAsync(tokenEntity);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {Username} registered successfully", user.Username);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = MapToUserDto(user, roles)
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var storedToken = await _unitOfWork.Repository<RefreshToken>()
            .Query()
            .Include(rt => rt.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (storedToken.RevokedAt != null)
            throw new UnauthorizedAccessException("Refresh token has been revoked.");

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired.");

        // Revoke old token (token rotation)
        storedToken.RevokedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<RefreshToken>().UpdateAsync(storedToken);

        var user = storedToken.User;
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var newAccessToken = _tokenProvider.GenerateAccessToken(user, roles);
        var newRefreshToken = JwtTokenProvider.GenerateRefreshToken();

        // Save new refresh token
        var newTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<RefreshToken>().AddAsync(newTokenEntity);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Token refreshed for user {Username}", user.Username);

        return new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = MapToUserDto(user, roles)
        };
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        var token = await _unitOfWork.Repository<RefreshToken>()
            .Query()
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken)
            ?? throw new InvalidOperationException("Token not found.");

        token.RevokedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<RefreshToken>().UpdateAsync(token);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<bool> ValidateBasicAuthAsync(string username, string password)
    {
        var user = await _unitOfWork.Repository<User>()
            .Query()
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null) return false;
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    private static UserDto MapToUserDto(User user, IEnumerable<string> roles) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Status = user.Status.ToString(),
        CreatedAt = user.CreatedAt,
        Roles = roles
    };
}
