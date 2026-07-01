namespace NotificationsAPI.Infrastructure.Persistence;

using NotificationsAPI.Domain.Shared;
using NotificationsAPI.Domain.Notifications;

/// <summary>
/// Implementation of Unit of Work pattern using Entity Framework Core.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private INotificationRepository? _notificationRepository;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets or creates the notification repository.
    /// </summary>
    public INotificationRepository Notifications
    {
        get
        {
            _notificationRepository ??= new NotificationRepository(_context);
            return _notificationRepository;
        }
    }

    /// <summary>
    /// Commits all changes to the database.
    /// </summary>
    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Disposes the DbContext.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}
