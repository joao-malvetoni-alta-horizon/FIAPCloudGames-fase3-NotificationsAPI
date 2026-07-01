namespace NotificationsAPI.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using NotificationsAPI.Domain.Notifications;

/// <summary>
/// Entity Framework Core DbContext for Notifications API.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Notifications DbSet.
    /// </summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Notification entity
        modelBuilder.Entity<Notification>(builder =>
        {
            builder.HasKey(n => n.Id);

            builder.Property(n => n.Id)
                .ValueGeneratedNever()
                .IsRequired();

            builder.Property(n => n.UserId)
                .IsRequired();

            builder.Property(n => n.Type)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(n => n.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(n => n.RecipientEmail)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(n => n.RecipientName)
                .HasMaxLength(255);

            builder.Property(n => n.Subject)
                .HasMaxLength(255);

            builder.Property(n => n.Body)
                .HasMaxLength(5000);

            builder.Property(n => n.EventId);

            builder.Property(n => n.RetryCount)
                .HasDefaultValue(0);

            builder.Property(n => n.LastError)
                .HasMaxLength(1000);

            builder.Property(n => n.CreatedAt)
                .IsRequired();

            builder.Property(n => n.UpdatedAt);

            // Create indexes for common queries
            builder.HasIndex(n => n.UserId)
                .HasDatabaseName("idx_notification_user_id");

            builder.HasIndex(n => n.Status)
                .HasDatabaseName("idx_notification_status");

            builder.HasIndex(n => n.EventId)
                .HasDatabaseName("idx_notification_event_id")
                .IsUnique();

            builder.HasIndex(n => n.CreatedAt)
                .HasDatabaseName("idx_notification_created_at");

            // Table name
            builder.ToTable("notifications");
        });
    }
}
