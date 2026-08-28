namespace Notifications.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Npgsql;
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
    /// <exception cref="DuplicateEventException">
    /// Quando a escrita viola o índice único de <c>Notification.EventId</c>, ou seja, quando o
    /// evento de integração já havia sido processado. Traduzir aqui mantém a dependência do
    /// PostgreSQL contida na camada de persistência: quem chama trata uma exceção de domínio e
    /// não precisa conhecer <c>DbUpdateException</c> nem <c>PostgresException</c>.
    /// </exception>
    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new DuplicateEventException(
                "O evento de integração já havia sido processado (violação de unicidade de EventId).",
                ex);
        }
    }

    /// <summary>
    /// Descarta o DbContext.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}
