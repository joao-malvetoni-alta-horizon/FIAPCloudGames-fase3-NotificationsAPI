namespace NotificationsAPI.Infrastructure.Email;

/// <summary>
/// Contrato para o serviço de notificações por email.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envia um email de boas-vindas para um novo usuário.
    /// </summary>
    /// <param name="email">Endereço de email do destinatário.</param>
    /// <param name="name">Nome do destinatário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task SendWelcomeEmailAsync(string email, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia um email de confirmação de compra de jogo.
    /// </summary>
    /// <param name="email">Endereço de email do destinatário.</param>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="gameId">Identificador do jogo que foi comprado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task SendPurchaseConfirmationAsync(string email, Guid userId, Guid gameId,
        CancellationToken cancellationToken = default);
}
