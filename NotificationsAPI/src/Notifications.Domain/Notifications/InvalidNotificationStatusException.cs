namespace Notifications.Domain.Notifications;

/// <summary>
/// Exceção lançada quando uma operação é executada em um status inválido.
/// </summary>
public class InvalidNotificationStatusException(
    NotificationStatus currentStatus,
    NotificationStatus expectedStatus,
    string operation) : NotificationDomainException(
    $"Operação '{operation}' não é permitida para notificações com status {currentStatus}. Status esperado: {expectedStatus}")
{
    public NotificationStatus CurrentStatus { get; } = currentStatus;
    public NotificationStatus ExpectedStatus { get; } = expectedStatus;
    public string Operation { get; } = operation;
}
