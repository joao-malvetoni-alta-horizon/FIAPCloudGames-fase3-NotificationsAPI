namespace Notifications.Domain.Notifications;

using Shared;

/// <summary>
/// Interface de repositório para o agregado de Notificação.
/// </summary>
public interface INotificationRepository : IRepository<Notification>
{
    /// <summary>
    /// Obtém todas as notificações de um usuário específico.
    /// </summary>
    /// <param name="userId">ID do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de notificações do usuário.</returns>
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém uma notificação pelo ID do evento originário (para idempotência).
    /// </summary>
    /// <param name="eventId">ID do evento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A notificação se encontrada; caso contrário, nulo.</returns>
    Task<Notification?> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
}
