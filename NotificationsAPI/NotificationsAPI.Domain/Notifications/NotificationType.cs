namespace NotificationsAPI.Domain.Notifications;

/// <summary>
/// Enumeration of notification types.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Welcome email sent when a new user is created.
    /// </summary>
    WelcomeEmail = 0,

    /// <summary>
    /// Confirmation email sent when a game purchase is completed.
    /// </summary>
    PurchaseConfirmation = 1
}
