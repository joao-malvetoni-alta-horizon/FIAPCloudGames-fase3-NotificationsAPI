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

}
