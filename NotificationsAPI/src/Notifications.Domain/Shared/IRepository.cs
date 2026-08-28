namespace Notifications.Domain.Shared;

/// <summary>
/// Interface genérica de repositório para acessar e modificar entidades.
/// </summary>
/// <typeparam name="T">Tipo de entidade que herda de Entity.</typeparam>
public interface IRepository<T> where T : Entity
{
    /// <summary>
    /// Adiciona uma nova entidade ao repositório.
    /// </summary>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
}
