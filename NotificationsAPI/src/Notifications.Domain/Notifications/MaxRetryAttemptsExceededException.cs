namespace Notifications.Domain.Notifications;

/// <summary>
/// Exceção lançada quando o número máximo de tentativas de reenvio é excedido.
/// </summary>
public class MaxRetryAttemptsExceededException(int currentRetryCount)
    : NotificationDomainException(
        $"Número máximo de tentativas ({MaxAttempts}) excedido. Tentativas atuais: {currentRetryCount}")
{
    public const int MaxAttempts = 3;

    public int CurrentRetryCount { get; } = currentRetryCount;
}
