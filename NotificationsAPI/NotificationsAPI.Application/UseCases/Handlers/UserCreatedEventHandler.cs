namespace NotificationsAPI.Application.UseCases.Handlers;

using Microsoft.Extensions.Logging;
using FiapCloudGames.Contracts.Users;
using Domain.Notifications;
using Domain.Shared;

/// <summary>
/// Manipulador de eventos para quando um usuário é criado por um administrador.
/// Cria uma notificação de boas-vindas para o novo usuário.
/// </summary>
public partial class UserCreatedEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<UserCreatedEventHandler> logger) : IEventHandler<UserCreatedEvent>
{
    public async Task HandleAsync(UserCreatedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            LogProcessingUserCreatedEvent(integrationEvent.UserId, integrationEvent.Name);

            var notification = Notification.Create(
                integrationEvent.UserId,
                NotificationType.WelcomeEmail,
                integrationEvent.Email,
                integrationEvent.Name,
                integrationEvent.EventId);

            await unitOfWork.Notifications.AddAsync(notification, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            LogWelcomeNotificationCreated(integrationEvent.UserId);
        }
        catch (Exception ex)
        {
            LogErrorProcessingUserCreatedEvent(ex, integrationEvent.UserId, ex.Message);
            throw;
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Processando evento UserCreatedEvent para usuário {UserId} ({UserName})")]
    private partial void LogProcessingUserCreatedEvent(Guid userId, string userName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Notificação de boas-vindas criada com sucesso para usuário {UserId}")]
    private partial void LogWelcomeNotificationCreated(Guid userId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Erro ao processar UserCreatedEvent para usuário {UserId}: {ErrorMessage}")]
    private partial void LogErrorProcessingUserCreatedEvent(Exception exception, Guid userId, string errorMessage);
}
