namespace Notifications.API.Models;

using NotificationsAPI.Domain.Notifications;

/// <summary>
/// Request para criar uma nova notificação.
/// </summary>
public record CreateNotificationRequest(
    Guid UserId,
    NotificationType Type,
    string RecipientEmail,
    string? RecipientName = null,
    Guid? EventId = null);

/// <summary>
/// Request para atualizar uma notificação.
/// </summary>
public record UpdateNotificationRequest(
    NotificationStatus? Status = null,
    string? LastError = null);

/// <summary>
/// Response com os dados de uma notificação.
/// </summary>
public record NotificationResponse(
    Guid Id,
    Guid UserId,
    NotificationType Type,
    NotificationStatus Status,
    string RecipientEmail,
    string? RecipientName,
    string? Subject,
    string? Body,
    Guid? EventId,
    int RetryCount,
    string? LastError,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public NotificationResponse(Notification notification) : this(
        notification.Id,
        notification.UserId,
        notification.Type,
        notification.Status,
        notification.RecipientEmail,
        notification.RecipientName,
        notification.Subject,
        notification.Body,
        notification.EventId,
        notification.RetryCount,
        notification.LastError,
        notification.CreatedAt,
        notification.UpdatedAt)
    {
    }
}
