using TaskForge.Application.DTOs;

namespace TaskForge.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task RevokeTokenAsync(string refreshToken);
    Task<bool> ValidateBasicAuthAsync(string username, string password);
}

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(int id);
    Task<PagedResult<UserDto>> GetAllAsync(int page = 1, int pageSize = 10);
    Task<UserDto> CreateAsync(CreateUserRequest request);
    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request);
    Task DeleteAsync(int id);
    Task AssignRoleAsync(int userId, int roleId);
    Task RemoveRoleAsync(int userId, int roleId);
}

public interface IRoleService
{
    Task<RoleDto?> GetByIdAsync(int id);
    Task<IEnumerable<RoleDto>> GetAllAsync();
    Task<RoleDto> CreateAsync(CreateRoleRequest request);
    Task DeleteAsync(int id);
}

public interface IProjectService
{
    Task<ProjectDto?> GetByIdAsync(int id);
    Task<PagedResult<ProjectDto>> GetAllAsync(int page = 1, int pageSize = 10);
    Task<ProjectDto> CreateAsync(int ownerId, CreateProjectRequest request);
    Task<ProjectDto> UpdateAsync(int id, UpdateProjectRequest request);
    Task DeleteAsync(int id);
}

public interface ITaskService
{
    Task<TaskItemDto?> GetByIdAsync(int id);
    Task<PagedResult<TaskItemDto>> GetAllAsync(int page = 1, int pageSize = 10, int? projectId = null, string? status = null);
    Task<TaskItemDto> CreateAsync(CreateTaskRequest request);
    Task<TaskItemDto> UpdateAsync(int id, UpdateTaskRequest request);
    Task DeleteAsync(int id);
    Task<IEnumerable<TaskItemDto>> BulkUpdateStatusAsync(BulkUpdateTaskStatusRequest request);
    Task<IEnumerable<TaskItemDto>> GetByUserAsync(int userId);
}

public interface IReportService
{
    Task<TaskSummaryReport> GetTaskSummaryAsync();
    Task<IEnumerable<ProjectTaskReport>> GetProjectTaskReportsAsync();
    Task<IEnumerable<UserProductivityReport>> GetUserProductivityAsync();
}

public interface ICacheService
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan? expiration = null);
    void Remove(string key);
}
