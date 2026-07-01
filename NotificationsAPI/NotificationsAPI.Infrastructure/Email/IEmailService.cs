namespace NotificationsAPI.Infrastructure.Email;

/// <summary>
/// Interface for email notification service.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a welcome email to a new user.
    /// </summary>
    /// <param name="email">Recipient email address.</param>
    /// <param name="name">Recipient name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendWelcomeEmailAsync(string email, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a purchase confirmation email.
    /// </summary>
    /// <param name="email">Recipient email address.</param>
    /// <param name="userId">User ID.</param>
    /// <param name="gameId">Game ID that was purchased.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPurchaseConfirmationAsync(
        string email,
        Guid userId,
        Guid gameId,
        CancellationToken cancellationToken = default);
}
