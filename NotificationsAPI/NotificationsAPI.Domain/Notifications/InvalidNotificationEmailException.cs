namespace NotificationsAPI.Domain.Notifications;

using Shared;

/// <summary>
/// Exceção lançada quando um email inválido é fornecido.
/// </summary>
public class InvalidNotificationEmailException : NotificationDomainException
{
    public InvalidNotificationEmailException(string email)
        : base($"Email inválido fornecido: '{email}'")
    {
        Email = email;
    }

    public string Email { get; }
}
