namespace Notifications.Domain.Shared;

/// <summary>
/// Sinaliza que ainda não existe notificação anterior do usuário para recuperar o e-mail de
/// destinatário (o UserRegisteredEvent provavelmente ainda não foi processado). Não é uma falha
/// real — é um estado transitório que se resolve com uma nova tentativa de entrega da fila.
/// </summary>
public class RecipientNotReadyException(Guid userId) : DomainException(
    $"Nenhuma notificação anterior encontrada para o usuário {userId}; o e-mail de destinatário " +
    "ainda não é conhecido. Provável corrida com o UserRegisteredEvent.")
{
    public Guid UserId { get; } = userId;
}
