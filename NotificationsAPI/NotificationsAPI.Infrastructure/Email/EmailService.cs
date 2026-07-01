namespace NotificationsAPI.Infrastructure.Email;

using Microsoft.Extensions.Logging;

/// <summary>
/// Simulated email service that logs notification messages.
/// In a real implementation, this would integrate with an SMTP provider or email service.
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendWelcomeEmailAsync(string email, string name, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "📧 Welcome email sent to {RecipientName} ({RecipientEmail})",
            name,
            email);

        return Task.CompletedTask;
    }

    public Task SendPurchaseConfirmationAsync(
        string email,
        Guid userId,
        Guid gameId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "📧 Purchase confirmation email sent to {RecipientEmail}. UserId: {UserId}, GameId: {GameId}",
            email,
            userId,
            gameId);

        return Task.CompletedTask;
    }
}
