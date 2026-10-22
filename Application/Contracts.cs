using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Domain;

namespace UserManagementAPI.Application;

public sealed record LoginRequest([property: Required, EmailAddress] string Email, [property: Required, MinLength(8)] string Password);
public sealed record CreateUserRequest([property: Required, StringLength(200)] string FullName, [property: Required, EmailAddress] string Email, [property: Required, MinLength(8)] string Password, UserRole Role = UserRole.User);
public sealed record UpdateUserRequest([property: Required, StringLength(200)] string FullName, [property: Required, EmailAddress] string Email, UserRole Role);
public sealed record UserResponse(int Id, string FullName, string Email, UserRole Role, DateTime CreatedAtUtc);
public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc, UserResponse User);
public sealed record CreateProjectRequest([property: Required, StringLength(200)] string Name, [property: StringLength(2000)] string? Description, ProjectStatus Status = ProjectStatus.Active);
public sealed record UpdateProjectRequest([property: Required, StringLength(200)] string Name, [property: StringLength(2000)] string? Description, ProjectStatus Status);
public sealed record AssignUserRequest(int UserId);
public sealed record ProjectResponse(int Id, string Name, string? Description, ProjectStatus Status, int CreatedByUserId, DateTime CreatedAtUtc, IReadOnlyList<UserResponse> Members);
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}

public interface IUserService
{
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
    Task<PagedResponse<UserResponse>> ListAsync(int page, int pageSize, string? search, CancellationToken cancellationToken);
}

public interface IProjectService
{
    Task<ProjectResponse> CreateAsync(int creatorId, CreateProjectRequest request, CancellationToken cancellationToken);
    Task<ProjectResponse> UpdateAsync(int id, UpdateProjectRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<ProjectResponse>> ListAsync(int page, int pageSize, ProjectStatus? status, CancellationToken cancellationToken);
    Task AssignUserAsync(int projectId, int userId, CancellationToken cancellationToken);
}

public sealed class ApiException : Exception
{
    public int StatusCode { get; }
    public ApiException(int statusCode, string message) : base(message) => StatusCode = statusCode;
}
