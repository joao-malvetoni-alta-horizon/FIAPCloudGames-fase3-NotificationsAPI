namespace NotificationsAPI.Domain.Shared;

/// <summary>
/// Base exception for domain-level errors. Represents violations of business rules.
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
