namespace NotificationsAPI.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Domain.Notifications;

/// <summary>
/// DbContext do Entity Framework Core para a API de Notificações.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>
    /// DbSet de Notificações.
    /// </summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
