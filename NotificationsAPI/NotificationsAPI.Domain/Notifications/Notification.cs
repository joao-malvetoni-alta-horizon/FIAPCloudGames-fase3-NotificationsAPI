namespace NotificationsAPI.Domain.Notifications;

using Shared;
using System.Text.RegularExpressions;

/// <summary>
/// Raiz de agregado de Notificação. Representa uma notificação enviada para um usuário.
/// </summary>
public class Notification : Entity
{
    private Notification()
    {
        // Para EF Core
    }

    /// <summary>
    /// ID do usuário que deve receber essa notificação.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Tipo de notificação.
    /// </summary>
    public NotificationType Type { get; private set; }

    /// <summary>
    /// Status atual de entrega.
    /// </summary>
    public NotificationStatus Status { get; private set; } = NotificationStatus.Pending;

    /// <summary>
    /// Endereço de email do destinatário.
    /// </summary>
    public string RecipientEmail { get; private set; } = string.Empty;

    /// <summary>
    /// Nome do destinatário.
    /// </summary>
    public string? RecipientName { get; private set; }

    /// <summary>
    /// Assunto da notificação.
    /// </summary>
    public string? Subject { get; private set; }

    /// <summary>
    /// Corpo da mensagem de notificação.
    /// </summary>
    public string? Body { get; private set; }

    /// <summary>
    /// ID do evento de integração originário (para idempotência).
    /// </summary>
    public Guid? EventId { get; private set; }

    /// <summary>
    /// Número de vezes que essa notificação foi retentada.
    /// </summary>
    public int RetryCount { get; private set; } = 0;

    /// <summary>
    /// Última mensagem de erro se o envio falhou.
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Método factory para criar uma nova notificação.
    /// </summary>
    /// <param name="userId">Usuário que deve receber a notificação.</param>
    /// <param name="type">Tipo de notificação.</param>
    /// <param name="recipientEmail">Endereço de email para enviar.</param>
    /// <param name="recipientName">Nome do destinatário.</param>
    /// <param name="eventId">ID do evento originário.</param>
    /// <returns>Uma nova instância de Notification.</returns>
    /// <exception cref="NotificationDomainException">Lançado se os parâmetros forem inválidos.</exception>
    public static Notification Create(
        Guid userId,
        NotificationType type,
        string recipientEmail,
        string? recipientName = null,
        Guid? eventId = null)
    {
        if (userId == Guid.Empty)
            throw new NotificationDomainException("UserId cannot be empty.");

        if (string.IsNullOrWhiteSpace(recipientEmail))
            throw new NotificationDomainException("RecipientEmail cannot be empty.");

        if (!IsValidEmail(recipientEmail))
            throw new NotificationDomainException("RecipientEmail is not in a valid format.");

        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            RecipientEmail = recipientEmail.Trim(),
            RecipientName = recipientName?.Trim(),
            Status = NotificationStatus.Pending,
            EventId = eventId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Marca essa notificação como enviada com sucesso.
    /// </summary>
    /// <exception cref="NotificationDomainException">Lançado se a notificação não estiver em status Pendente.</exception>
    public void MarkAsSent()
    {
        if (Status != NotificationStatus.Pending)
            throw new NotificationDomainException(
                $"Apenas notificações Pendentes podem ser marcadas como Enviadas. Status atual: {Status}");

        Status = NotificationStatus.Sent;
        UpdatedAt = DateTime.UtcNow;
        LastError = null;
    }

    /// <summary>
    /// Marca essa notificação como falhada.
    /// </summary>
    /// <param name="errorMessage">Mensagem de erro descrevendo por que falhou.</param>
    /// <exception cref="NotificationDomainException">Lançado se a notificação não estiver em status Pendente.</exception>
    public void MarkAsFailed(string errorMessage)
    {
        if (Status != NotificationStatus.Pending)
            throw new NotificationDomainException(
                $"Apenas notificações Pendentes podem ser marcadas como Falhadas. Status atual: {Status}");

        Status = NotificationStatus.Failed;
        LastError = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca essa notificação como entregue.
    /// </summary>
    /// <exception cref="NotificationDomainException">Lançado se a notificação não estiver em status Enviado.</exception>
    public void MarkAsDelivered()
    {
        if (Status != NotificationStatus.Sent)
            throw new NotificationDomainException(
                $"Apenas notificações Enviadas podem ser marcadas como Entregues. Status atual: {Status}");

        Status = NotificationStatus.Delivered;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Incrementa a contagem de tentativas.
    /// </summary>
    public void IncrementRetryCount()
    {
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Redefine a notificação para status Pendente para retentar.
    /// </summary>
    public void ResetForRetry()
    {
        if (RetryCount >= 3)
            throw new NotificationDomainException("Número máximo de tentativas ultrapassado.");

        Status = NotificationStatus.Pending;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Valida se o endereço de email fornecido está em um formato válido.
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        try
        {
            var emailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
            return Regex.IsMatch(email, emailPattern);
        }
        catch
        {
            return false;
        }
    }
}
