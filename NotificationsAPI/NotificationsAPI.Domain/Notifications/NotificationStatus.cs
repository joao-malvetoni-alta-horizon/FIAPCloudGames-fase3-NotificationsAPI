namespace NotificationsAPI.Domain.Notifications;

/// <summary>
/// Enumeration of notification delivery statuses.
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// Notification is pending processing.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Notification has been successfully sent.
    /// </summary>
    Sent = 1,

    /// <summary>
    /// Notification failed to send.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Notification has been delivered to recipient.
    /// </summary>
    Delivered = 3
}
