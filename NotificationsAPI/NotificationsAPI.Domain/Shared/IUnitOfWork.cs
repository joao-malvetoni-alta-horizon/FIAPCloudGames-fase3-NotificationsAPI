namespace NotificationsAPI.Domain.Shared;

/// <summary>
/// Interface for unit of work pattern. Manages transaction boundaries across repositories.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>
    /// Commits all changes made through repositories to the persistence store.
    /// </summary>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
