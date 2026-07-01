namespace NotificationsAPI.Domain.Notifications;

using Shared;
using System.Text.RegularExpressions;

/// <summary>
/// Raiz de agregado de Notificação. Representa uma notificação enviada para um usuário.
/// </summary>
public partial class Notification : Entity
{
    private Notification() { }

    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public NotificationStatus Status { get; private set; } = NotificationStatus.Pending;
    public string RecipientEmail { get; private set; } = string.Empty;
    public string? RecipientName { get; private set; }
    public string? Subject { get; init; }
    public string? Body { get; init; }
    public Guid? EventId { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }

    /// <param name="userId">Usuário que deve receber a notificação.</param>
    /// <param name="type">Tipo de notificação.</param>
    /// <param name="recipientEmail">Endereço de email para enviar.</param>
    /// <param name="recipientName">Nome do destinatário.</param>
    /// <param name="eventId">ID do evento originário (para idempotência).</param>
    /// <exception cref="InvalidNotificationEmailException">Email inválido.</exception>
    public static Notification Create(
        Guid userId,
        NotificationType type,
        string recipientEmail,
        string? recipientName = null,
        Guid? eventId = null)
    {
        ValidateUserId(userId);
        EnsureIsNotNullOrEmpty(recipientEmail);
        string recipientEmailTrimmed = recipientEmail.Trim();
        ValidateUserEmail(recipientEmailTrimmed);

        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            RecipientEmail = recipientEmailTrimmed,
            RecipientName = recipientName?.Trim(),
            Status = NotificationStatus.Pending,
            EventId = eventId,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static void EnsureIsNotNullOrEmpty(string recipientEmail)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            throw new NotificationDomainException("RecipientEmail cannot be empty.");
        }
    }

    private static void ValidateUserEmail(string recipientEmail)
    {
        if (!IsValidEmail(recipientEmail))
        {
            throw new InvalidNotificationEmailException(recipientEmail);
        }
    }

    private static void ValidateUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new NotificationDomainException("UserId cannot be empty.");
        }
    }

    /// <exception cref="InvalidNotificationStatusException">Status inválido para esta operação.</exception>
    public void MarkAsSent()
    {
        if (Status != NotificationStatus.Pending)
        {
            throw new InvalidNotificationStatusException(Status, NotificationStatus.Pending, nameof(MarkAsSent));
        }

        Status = NotificationStatus.Sent;
        UpdatedAt = DateTime.UtcNow;
        LastError = null;
    }

    /// <exception cref="InvalidNotificationStatusException">Status inválido para esta operação.</exception>
    public void MarkAsFailed(string errorMessage)
    {
        if (Status != NotificationStatus.Pending)
        {
            throw new InvalidNotificationStatusException(Status, NotificationStatus.Pending, nameof(MarkAsFailed));
        }

        Status = NotificationStatus.Failed;
        LastError = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <exception cref="InvalidNotificationStatusException">Status inválido para esta operação.</exception>
    public void MarkAsDelivered()
    {
        if (Status != NotificationStatus.Sent)
        {
            throw new InvalidNotificationStatusException(Status, NotificationStatus.Sent, nameof(MarkAsDelivered));
        }

        Status = NotificationStatus.Delivered;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementRetryCount()
    {
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <exception cref="MaxRetryAttemptsExceededException">Número máximo de tentativas excedido.</exception>
    public void ResetForRetry()
    {
        if (RetryCount >= MaxRetryAttemptsExceededException.MaxAttempts)
        {
            throw new MaxRetryAttemptsExceededException(RetryCount);
        }

        Status = NotificationStatus.Pending;
        UpdatedAt = DateTime.UtcNow;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            return EmailPattern().IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
    private static partial Regex EmailPattern();
}
