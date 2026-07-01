namespace NotificationsAPI.Infrastructure.Persistence;

using NotificationsAPI.Domain.Shared;
using NotificationsAPI.Domain.Notifications;

/// <summary>
/// Implementação do padrão Unit of Work usando Entity Framework Core.
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
    /// Obtém ou cria o repositório de notificações.
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
    /// Confirma todas as mudanças no banco de dados.
    /// </summary>
    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Descarta o DbContext.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}
