namespace NotificationsAPI.Domain.Shared;

/// <summary>
/// Interface genérica de repositório para acessar e modificar entidades.
/// </summary>
/// <typeparam name="T">Tipo de entidade que herda de Entity.</typeparam>
public interface IRepository<T> where T : Entity
{
    /// <summary>
    /// Obtém uma entidade pelo seu identificador único.
    /// </summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todas as entidades do tipo T.
    /// </summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona uma nova entidade ao repositório.
    /// </summary>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza uma entidade existente.
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Remove uma entidade do repositório.
    /// </summary>
    void Delete(T entity);
}