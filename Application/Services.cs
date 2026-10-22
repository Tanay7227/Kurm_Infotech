using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UserManagementAPI.Domain;
using UserManagementAPI.Infrastructure;

namespace UserManagementAPI.Application;

public sealed class AuthService(AppDbContext db, IConfiguration configuration) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == request.Email && !x.IsDeleted, cancellationToken);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new ApiException(StatusCodes.Status401Unauthorized, "Invalid email or password.");

        var expires = DateTime.UtcNow.AddMinutes(configuration.GetValue<int>("Jwt:ExpirationMinutes", 60));
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(configuration["Jwt:Issuer"], configuration["Jwt:Audience"], claims, expires: expires, signingCredentials: credentials);
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, Map(user));
    }

    internal static UserResponse Map(User user) => new(user.Id, user.FullName, user.Email, user.Role, user.CreatedAtUtc);
}

public sealed class UserService(AppDbContext db) : IUserService
{
    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
            throw new ApiException(StatusCodes.Status409Conflict, "A user with this email already exists.");
        var user = new User { FullName = request.FullName.Trim(), Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), Role = request.Role };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return AuthService.Map(user);
    }

    public async Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken) ?? throw new ApiException(404, "User not found.");
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Id != id && x.Email == email, cancellationToken))
            throw new ApiException(409, "A user with this email already exists.");
        user.FullName = request.FullName.Trim(); user.Email = email; user.Role = request.Role; user.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return AuthService.Map(user);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken) ?? throw new ApiException(404, "User not found.");
        user.IsDeleted = true; user.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResponse<UserResponse>> ListAsync(int page, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.FullName.Contains(search) || x.Email.Contains(search));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new UserResponse(x.Id, x.FullName, x.Email, x.Role, x.CreatedAtUtc)).ToListAsync(cancellationToken);
        return new PagedResponse<UserResponse>(items, page, pageSize, total);
    }
}

public sealed class ProjectService(AppDbContext db) : IProjectService
{
    public async Task<ProjectResponse> CreateAsync(int creatorId, CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = new Project { Name = request.Name.Trim(), Description = request.Description, Status = request.Status, CreatedByUserId = creatorId };
        db.Projects.Add(project); await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(project.Id, cancellationToken);
    }

    public async Task<ProjectResponse> UpdateAsync(int id, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.FindAsync([id], cancellationToken) ?? throw new ApiException(404, "Project not found.");
        project.Name = request.Name.Trim(); project.Description = request.Description; project.Status = request.Status; project.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken); return await GetAsync(id, cancellationToken);
    }

    public async Task<PagedResponse<ProjectResponse>> ListAsync(int page, int pageSize, ProjectStatus? status, CancellationToken cancellationToken)
    {
        var query = db.Projects.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        var total = await query.CountAsync(cancellationToken);
        var projects = await query.Include(x => x.Members).ThenInclude(x => x.User).OrderBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var items = projects.Select(Map).ToList();
        return new PagedResponse<ProjectResponse>(items, page, pageSize, total);
    }

    public async Task AssignUserAsync(int projectId, int userId, CancellationToken cancellationToken)
    {
        if (!await db.Projects.AnyAsync(x => x.Id == projectId, cancellationToken)) throw new ApiException(404, "Project not found.");
        if (!await db.Users.AnyAsync(x => x.Id == userId && !x.IsDeleted, cancellationToken)) throw new ApiException(404, "Active user not found.");
        if (!await db.ProjectMembers.AnyAsync(x => x.ProjectId == projectId && x.UserId == userId, cancellationToken))
        { db.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = userId }); await db.SaveChangesAsync(cancellationToken); }
    }

    private async Task<ProjectResponse> GetAsync(int id, CancellationToken cancellationToken)
    {
        var project = await db.Projects.AsNoTracking().Include(x => x.Members).ThenInclude(x => x.User).SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new ApiException(404, "Project not found.");
        return Map(project);
    }

    private static ProjectResponse Map(Project project) => new(project.Id, project.Name, project.Description, project.Status, project.CreatedByUserId, project.CreatedAtUtc, project.Members.Where(x => x.User is not null && !x.User.IsDeleted).Select(x => AuthService.Map(x.User!)).ToList());
}
