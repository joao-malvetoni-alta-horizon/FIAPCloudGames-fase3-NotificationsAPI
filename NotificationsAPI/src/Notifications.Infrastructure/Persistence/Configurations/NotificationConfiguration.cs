namespace NotificationsAPI.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Notifications;

/// <summary>
/// Configuração do Entity Framework Core para a entidade Notification.
/// </summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    /// <summary>
    /// Configura a entidade Notification no modelo de banco de dados.
    /// </summary>
    /// <param name="builder">Builder para a configuração da entidade.</param>
    public void Configure(EntityTypeBuilder<Notification> builder)
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

        builder.HasIndex(n => n.UserId)
            .HasDatabaseName("idx_notification_user_id");

        builder.HasIndex(n => n.Status)
            .HasDatabaseName("idx_notification_status");

        builder.HasIndex(n => n.EventId)
            .HasDatabaseName("idx_notification_event_id")
            .IsUnique();

        builder.HasIndex(n => n.CreatedAt)
            .HasDatabaseName("idx_notification_created_at");

        builder.ToTable("notifications");
    }
}
