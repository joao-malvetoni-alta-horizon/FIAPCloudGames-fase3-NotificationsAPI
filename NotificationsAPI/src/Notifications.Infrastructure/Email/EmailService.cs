namespace Notifications.Infrastructure.Email;

using Microsoft.Extensions.Logging;

/// <summary>
/// Serviço de email simulado que registra mensagens de notificação em logs.
/// Em uma implementação real, isso se integraria com um provedor SMTP ou serviço de email.
/// </summary>
public partial class EmailService(ILogger<EmailService> logger) : IEmailService
{
    /// <summary>
    /// Envia um email de boas-vindas para um novo usuário.
    /// </summary>
    public Task SendWelcomeEmailAsync(string email, string name, CancellationToken cancellationToken = default)
    {
        LogWelcomeEmailSent(name, email);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Envia um email de confirmação de compra de jogo.
    /// </summary>
    public Task SendPurchaseConfirmationAsync(
        string email,
        Guid userId,
        Guid gameId,
        CancellationToken cancellationToken = default)
    {
        LogPurchaseConfirmationEmailSent(email, userId, gameId);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Email de boas-vindas enviado para {RecipientName} ({RecipientEmail})")]
    private partial void LogWelcomeEmailSent(string recipientName, string recipientEmail);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Email de confirmação de compra enviado para {RecipientEmail}. UserId: {UserId}, GameId: {GameId}")]
    private partial void LogPurchaseConfirmationEmailSent(string recipientEmail, Guid userId, Guid gameId);
}
