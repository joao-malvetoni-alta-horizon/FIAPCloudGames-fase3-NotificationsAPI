namespace NotificationsAPI.Domain.Notifications;

/// <summary>
/// Exceção lançada quando um email inválido é fornecido.
/// </summary>
public class InvalidNotificationEmailException(string email)
    : NotificationDomainException($"Email inválido fornecido: '{email}'")
{
    public string Email { get; } = email;
}
