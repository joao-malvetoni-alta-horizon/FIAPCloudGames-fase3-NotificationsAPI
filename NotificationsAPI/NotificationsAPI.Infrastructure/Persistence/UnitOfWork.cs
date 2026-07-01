namespace NotificationsAPI.Infrastructure.Persistence;

using Domain.Shared;

/// <summary>
/// Implementação do padrão Unit of Work usando Entity Framework Core.
/// </summary>
public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private readonly AppDbContext _context = context;

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
