namespace NotificationsAPI.Domain.Shared;

/// <summary>
/// Generic repository interface for accessing and modifying entities.
/// </summary>
/// <typeparam name="T">Entity type that inherits from Entity.</typeparam>
public interface IRepository<T> where T : Entity
{
    /// <summary>
    /// Gets an entity by its unique identifier.
    /// </summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities of type T.
    /// </summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    void Delete(T entity);
}
