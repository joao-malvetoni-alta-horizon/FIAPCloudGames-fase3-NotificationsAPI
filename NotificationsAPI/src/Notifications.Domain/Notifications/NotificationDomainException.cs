namespace Notifications.Domain.Notifications;

using Shared;

/// <summary>
/// Exceção lançada quando uma regra de negócio do domínio de notificação é violada.
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
