using Microsoft.EntityFrameworkCore;
using UserManagementAPI.Domain;

namespace UserManagementAPI.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        });
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.HasKey(x => new { x.ProjectId, x.UserId });
            entity.HasOne(x => x.Project).WithMany(x => x.Members).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User).WithMany(x => x.ProjectMemberships).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);
        if (await db.Users.AnyAsync(cancellationToken)) return;
        var admin = new User { FullName = "System Administrator", Email = "admin@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe123!"), Role = UserRole.Admin };
        var user = new User { FullName = "Demo User", Email = "user@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe123!"), Role = UserRole.User };
        db.Users.AddRange(admin, user);
        var project = new Project { Name = "API Modernization", Description = "Seed project for local development.", CreatedByUser = admin };
        db.Projects.Add(project);
        db.ProjectMembers.Add(new ProjectMember { Project = project, User = user });
        await db.SaveChangesAsync(cancellationToken);
    }
}
