namespace NotificationsAPI.Application.UseCases.Handlers;

using Microsoft.Extensions.Logging;
using FiapCloudGames.Contracts.Users;
using Domain.Notifications;
using Domain.Shared;

/// <summary>
/// Manipulador de eventos para quando um usuário se auto-registra na plataforma.
/// Cria uma notificação de boas-vindas para o novo usuário.
/// </summary>
public class UserRegisteredEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<UserRegisteredEventHandler> logger) : IEventHandler<UserRegisteredEvent>
{
    public async Task HandleAsync(UserRegisteredEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Processando evento UserRegisteredEvent para usuário {UserId} ({UserName})",
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
                "Erro ao processar UserRegisteredEvent para usuário {UserId}: {ErrorMessage}",
                @event.UserId,
                ex.Message);
            throw;
        }
    }
}
