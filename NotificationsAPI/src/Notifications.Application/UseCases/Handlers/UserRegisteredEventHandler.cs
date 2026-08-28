namespace Notifications.Application.UseCases.Handlers;

using Microsoft.Extensions.Logging;
using FiapCloudGames.Contracts.Users;
using Domain.Notifications;

/// <summary>
/// Manipulador de eventos para quando um usuário se auto-registra na plataforma.
/// Cria uma notificação de boas-vindas para o novo usuário e envia o email correspondente.
/// </summary>
public partial class UserRegisteredEventHandler(
    INotificationRepository notificationRepository,
    IEmailService emailService,
    ILogger<UserRegisteredEventHandler> logger) : IEventHandler<UserRegisteredEvent>
{
    public async Task HandleAsync(UserRegisteredEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            LogProcessingUserRegisteredEvent(integrationEvent.UserId, integrationEvent.Name);

            var notification = Notification.Create(
                integrationEvent.UserId,
                NotificationType.WelcomeEmail,
                integrationEvent.Email,
                integrationEvent.Name,
                integrationEvent.EventId);

            await notificationRepository.AddAsync(notification, cancellationToken);

            LogWelcomeNotificationCreated(integrationEvent.UserId);

            await emailService.SendWelcomeEmailAsync(integrationEvent.Email, integrationEvent.Name, cancellationToken);
        }
        catch (Exception ex)
        {
            LogErrorProcessingUserRegisteredEvent(ex, integrationEvent.UserId, ex.Message);
            throw;
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Processando evento UserRegisteredEvent para usuário {UserId} ({UserName})")]
    private partial void LogProcessingUserRegisteredEvent(Guid userId, string userName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Notificação de boas-vindas criada com sucesso para usuário {UserId}")]
    private partial void LogWelcomeNotificationCreated(Guid userId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Erro ao processar UserRegisteredEvent para usuário {UserId}: {ErrorMessage}")]
    private partial void LogErrorProcessingUserRegisteredEvent(Exception exception, Guid userId, string errorMessage);
}
