namespace Notifications.Domain.Notifications;

/// <summary>
/// Serviço de envio de emails relacionados a notificações.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envia um email de boas-vindas para um novo usuário.
    /// </summary>
    Task SendWelcomeEmailAsync(string email, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia um email de confirmação de compra de jogo.
    /// </summary>
    Task SendPurchaseConfirmationAsync(
        string email,
        Guid userId,
        Guid gameId,
        CancellationToken cancellationToken = default);
}
