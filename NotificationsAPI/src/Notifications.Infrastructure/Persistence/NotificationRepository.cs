namespace Notifications.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Domain.Notifications;

/// <summary>
/// Implementação de repositório para o agregado de Notificação usando EF Core.
/// </summary>
public class NotificationRepository(AppDbContext context) : INotificationRepository
{
    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task AddAsync(Notification entity, CancellationToken cancellationToken = default)
    {
        await context.Notifications.AddAsync(entity, cancellationToken);
    }

    public void Update(Notification entity)
    {
        context.Notifications.Update(entity);
    }

    public void Delete(Notification entity)
    {
        context.Notifications.Remove(entity);
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Notification?> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return await context.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.EventId == eventId, cancellationToken);
    }
}
