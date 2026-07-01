namespace NotificationsAPI.Application.UseCases.Handlers;

using Microsoft.Extensions.Logging;
using FiapCloudGames.Contracts.Users;
using Domain.Notifications;
using Domain.Shared;

/// <summary>
/// Manipulador de eventos para quando um usuário é criado por um administrador.
/// Cria uma notificação de boas-vindas para o novo usuário.
/// </summary>
public class UserCreatedEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<UserCreatedEventHandler> logger) : IEventHandler<UserCreatedEvent>
{
    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Processando evento UserCreatedEvent para usuário {UserId} ({UserName})",
                @event.UserId,
                @event.Name);

            var notification = Notification.Create(
                userId: @event.UserId,
                type: NotificationType.WelcomeEmail,
                recipientEmail: @event.Email,
                recipientName: @event.Name,
                eventId: @event.EventId);

            await unitOfWork.Notifications.AddAsync(notification, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Notificação de boas-vindas criada com sucesso para usuário {UserId}",
                @event.UserId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Erro ao processar UserCreatedEvent para usuário {UserId}: {ErrorMessage}",
                @event.UserId,
                ex.Message);
            throw;
        }
    }
}
