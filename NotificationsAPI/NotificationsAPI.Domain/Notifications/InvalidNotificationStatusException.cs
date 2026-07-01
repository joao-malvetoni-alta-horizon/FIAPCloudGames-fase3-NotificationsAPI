namespace NotificationsAPI.Domain.Notifications;

using Shared;

/// <summary>
/// Exceção lançada quando uma operação é executada em um status inválido.
/// </summary>
public class InvalidNotificationStatusException : NotificationDomainException
{
    public InvalidNotificationStatusException(NotificationStatus currentStatus, NotificationStatus expectedStatus, string operation)
        : base($"Operação '{operation}' não é permitida para notificações com status {currentStatus}. Status esperado: {expectedStatus}")
    {
        CurrentStatus = currentStatus;
        ExpectedStatus = expectedStatus;
        Operation = operation;
    }

    public NotificationStatus CurrentStatus { get; }
    public NotificationStatus ExpectedStatus { get; }
    public string Operation { get; }
}
