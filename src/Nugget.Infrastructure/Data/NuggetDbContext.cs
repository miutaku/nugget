using Microsoft.EntityFrameworkCore;
using Nugget.Core.Entities;

namespace Nugget.Infrastructure.Data;

/// <summary>
/// Nugget データベースコンテキスト
/// </summary>
public class NuggetDbContext : DbContext
{
    public NuggetDbContext(DbContextOptions<NuggetDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Todo> Todos => Set<Todo>();
    public DbSet<TodoAssignment> TodoAssignments => Set<TodoAssignment>();
    public DbSet<NotificationSetting> NotificationSettings => Set<NotificationSetting>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
            entity.Property(e => e.SlackUserId).HasColumnName("slack_user_id").HasMaxLength(64);
            entity.Property(e => e.Role).HasColumnName("role").HasConversion<string>();
            entity.Property(e => e.SamlNameId).HasColumnName("saml_name_id").HasMaxLength(512);
            entity.Property(e => e.ExternalId).HasColumnName("external_id").HasMaxLength(256);
            entity.Property(e => e.Department).HasColumnName("department").HasMaxLength(256);
            entity.Property(e => e.Division).HasColumnName("division").HasMaxLength(256);
            entity.Property(e => e.JobTitle).HasColumnName("job_title").HasMaxLength(256);
            entity.Property(e => e.EmployeeNumber).HasColumnName("employee_number").HasMaxLength(128);
            entity.Property(e => e.CostCenter).HasColumnName("cost_center").HasMaxLength(256);
            entity.Property(e => e.Organization).HasColumnName("organization").HasMaxLength(256);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.SamlNameId);
            entity.HasIndex(e => e.ExternalId);
            entity.HasIndex(e => e.Department);
        });

        // Todo configuration
        modelBuilder.Entity<Todo>(entity =>
        {
            entity.ToTable("todos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(512).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DueDate).HasColumnName("due_date").IsRequired();
            entity.Property(e => e.CreatedById).HasColumnName("created_by");
            entity.Property(e => e.TargetType).HasColumnName("target_type").HasConversion<string>();
            entity.Property(e => e.TargetGroupName).HasColumnName("target_group_name").HasMaxLength(256);
            entity.Property(e => e.TargetGroupId).HasColumnName("target_group_id");
            entity.Property(e => e.TargetAttributeKey).HasColumnName("target_attribute_key").HasMaxLength(64);
            entity.Property(e => e.TargetAttributeValue).HasColumnName("target_attribute_value").HasMaxLength(256);
            entity.Property(e => e.NotifyImmediately).HasColumnName("notify_immediately").HasDefaultValue(true);
            entity.Property(e => e.ReminderDays).HasColumnName("reminder_days");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.CreatedBy)
                .WithMany(u => u.CreatedTodos)
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TargetGroup)
                .WithMany()
                .HasForeignKey(e => e.TargetGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.DueDate);
        });

        // TodoAssignment configuration
        modelBuilder.Entity<TodoAssignment>(entity =>
        {
            entity.ToTable("todo_assignments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TodoId).HasColumnName("todo_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.IsCompleted).HasColumnName("is_completed").HasDefaultValue(false);
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.LastNotifiedAt).HasColumnName("last_notified_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Todo)
                .WithMany(t => t.Assignments)
                .HasForeignKey(e => e.TodoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Assignments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TodoId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.UserId);
        });

        // NotificationSetting configuration
        modelBuilder.Entity<NotificationSetting>(entity =>
        {
            entity.ToTable("notification_settings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.DaysBeforeDue).HasColumnName("days_before_due");
            entity.Property(e => e.SlackNotificationEnabled).HasColumnName("slack_notification_enabled").HasDefaultValue(true);
            entity.Property(e => e.NotificationHour).HasColumnName("notification_hour").HasDefaultValue(9);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.User)
                .WithOne(u => u.NotificationSetting)
                .HasForeignKey<NotificationSetting>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Group configuration
        modelBuilder.Entity<Group>(entity =>
        {
            entity.ToTable("groups");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
            entity.Property(e => e.ExternalId).HasColumnName("external_id").HasMaxLength(256);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.ExternalId);
        });

        // UserGroup configuration
        modelBuilder.Entity<UserGroup>(entity =>
        {
            entity.ToTable("user_groups");
            entity.HasKey(e => new { e.UserId, e.GroupId });
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserGroups)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Group)
                .WithMany(g => g.UserGroups)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var now = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is User user)
                {
                    user.CreatedAt = now;
                    user.UpdatedAt = now;
                }
                else if (entry.Entity is Todo todo)
                {
                    todo.CreatedAt = now;
                    todo.UpdatedAt = now;
                }
                else if (entry.Entity is TodoAssignment assignment)
                {
                    assignment.CreatedAt = now;
                }
                else if (entry.Entity is NotificationSetting setting)
                {
                    setting.CreatedAt = now;
                    setting.UpdatedAt = now;
                }
                else if (entry.Entity is Group group)
                {
                    group.CreatedAt = now;
                    group.UpdatedAt = now;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is User user)
                {
                    user.UpdatedAt = now;
                }
                else if (entry.Entity is Todo todo)
                {
                    todo.UpdatedAt = now;
                }
                else if (entry.Entity is NotificationSetting setting)
                {
                    setting.UpdatedAt = now;
                }
                else if (entry.Entity is Group group)
                {
                    group.UpdatedAt = now;
                }
            }
        }
    }
}
