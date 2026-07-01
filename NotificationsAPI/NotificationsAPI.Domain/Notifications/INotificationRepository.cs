namespace NotificationsAPI.Domain.Notifications;

using Shared;

/// <summary>
/// Repository interface for Notification aggregate.
/// </summary>
public interface INotificationRepository : IRepository<Notification>
{
    /// <summary>
    /// Gets all notifications for a specific user.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of notifications for the user.</returns>
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all notifications with a specific status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of notifications with the specified status.</returns>
    Task<IReadOnlyList<Notification>> GetByStatusAsync(
        NotificationStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a notification by its originating event ID (for idempotency).
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The notification if found; otherwise null.</returns>
    Task<Notification?> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
}
