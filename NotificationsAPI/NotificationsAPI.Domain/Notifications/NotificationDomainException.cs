namespace NotificationsAPI.Domain.Notifications;

using Shared;

/// <summary>
/// Exception thrown when a notification domain business rule is violated.
/// </summary>
public class NotificationDomainException : DomainException
{
    public NotificationDomainException(string message) : base(message)
    {
    }

    public NotificationDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
