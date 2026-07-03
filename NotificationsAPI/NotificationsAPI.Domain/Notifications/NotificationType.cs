namespace NotificationsAPI.Domain.Notifications;

/// <summary>
/// Enumeração dos tipos de notificação.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Email de boas-vindas enviado quando um novo usuário é criado.
    /// </summary>
    WelcomeEmail = 0,

    /// <summary>
    /// Email de confirmação enviado quando uma compra de jogo é concluída.
    /// </summary>
    PurchaseConfirmation = 1
}
