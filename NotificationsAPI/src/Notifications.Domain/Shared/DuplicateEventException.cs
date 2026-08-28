namespace Notifications.Domain.Shared;

/// <summary>
/// Sinaliza que um evento de integração já processado anteriormente foi recebido novamente
/// (violação da restrição de unicidade de <c>EventId</c>). Representa uma reentrega idempotente
/// do broker de mensagens, não uma falha real de processamento.
/// </summary>
public class DuplicateEventException : DomainException
{
    public DuplicateEventException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
