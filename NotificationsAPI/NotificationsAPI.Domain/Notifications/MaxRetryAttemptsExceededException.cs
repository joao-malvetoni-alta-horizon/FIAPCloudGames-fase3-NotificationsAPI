namespace NotificationsAPI.Domain.Notifications;

using Shared;

/// <summary>
/// Exceção lançada quando o número máximo de tentativas de reenvio é excedido.
/// </summary>
public class MaxRetryAttemptsExceededException : NotificationDomainException
{
    public const int MaxAttempts = 3;

    public MaxRetryAttemptsExceededException(int currentRetryCount)
        : base($"Número máximo de tentativas ({MaxAttempts}) excedido. Tentativas atuais: {currentRetryCount}")
    {
        CurrentRetryCount = currentRetryCount;
    }

    public int CurrentRetryCount { get; }
}
