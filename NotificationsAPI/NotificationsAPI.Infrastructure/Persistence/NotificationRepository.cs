namespace NotificationsAPI.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using NotificationsAPI.Domain.Notifications;
using NotificationsAPI.Domain.Shared;

/// <summary>
/// Repository implementation for Notification aggregate using EF Core.
/// </summary>
public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Notification entity, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(entity, cancellationToken);
    }

    public void Update(Notification entity)
    {
        _context.Notifications.Update(entity);
    }

    public void Delete(Notification entity)
    {
        _context.Notifications.Remove(entity);
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetByStatusAsync(
        NotificationStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.Status == status)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Notification?> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.EventId == eventId, cancellationToken);
    }
}
