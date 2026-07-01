namespace NotificationsAPI.Domain.Shared;

/// <summary>
/// Classe base para todas as entidades do domínio. Fornece identidade e comparação de igualdade.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Identificador único desta entidade.
    /// </summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>
    /// Momento de criação da entidade (UTC).
    /// </summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// Momento da última atualização da entidade (UTC). Nulo se nunca foi atualizado.
    /// </summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// Duas entidades são iguais se possuem o mesmo Id.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is Entity other && (ReferenceEquals(this, other) ||
                                       (Id != Guid.Empty && other.Id != Guid.Empty && Id == other.Id));
    }

    /// <summary>
    /// Código hash baseado no Id da entidade.
    /// </summary>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity? left, Entity? right)
    {
        return (left is null && right is null) || (left is not null && right is not null && left.Equals(right));
    }

    public static bool operator !=(Entity? left, Entity? right)
    {
        return !(left == right);
    }
}
