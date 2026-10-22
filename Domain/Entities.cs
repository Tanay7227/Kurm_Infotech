namespace UserManagementAPI.Domain;

public enum UserRole
{
    User,
    Admin
}

public enum ProjectStatus
{
    Active,
    OnHold,
    Completed
}

public sealed class User
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
}

public sealed class Project
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
}

public sealed class ProjectMember
{
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}
