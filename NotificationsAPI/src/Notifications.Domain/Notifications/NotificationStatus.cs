namespace Notifications.Domain.Notifications;

/// <summary>
/// Enumeração dos status de entrega de notificações.
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// Notificação está pendente de processamento.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Notificação foi enviada com sucesso.
    /// </summary>
    Sent = 1,

    /// <summary>
    /// Falha no envio da notificação.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Notificação foi entregue ao destinatário.
    /// </summary>
    Delivered = 3
}
