namespace NotificationsAPI.Domain.Shared;

/// <summary>
/// Interface para o padrão unit of work. Gerencia limites de transação entre repositórios.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>
    /// Confirma todas as mudanças feitas através dos repositórios no armazenamento de persistência.
    /// </summary>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}