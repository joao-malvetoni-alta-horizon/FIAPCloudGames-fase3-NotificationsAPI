namespace Notifications.Domain.Shared;

/// <summary>
/// Exceção base para erros de nível de domínio. Representa violações de regras de negócio.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
