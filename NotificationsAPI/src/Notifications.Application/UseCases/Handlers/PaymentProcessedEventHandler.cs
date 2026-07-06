namespace Notifications.Application.UseCases.Handlers;

using Microsoft.Extensions.Logging;
using Notifications.Domain.Shared;
using FiapCloudGames.Contracts.Payments;
using Domain.Notifications;

/// <summary>
/// Manipulador de eventos para quando um pagamento é processado. Se aprovado, cria uma
/// notificação de confirmação de compra e envia o email correspondente.
/// </summary>
public partial class PaymentProcessedEventHandler(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    ILogger<PaymentProcessedEventHandler> logger) : IEventHandler<PaymentProcessedEvent>
{
    public async Task HandleAsync(PaymentProcessedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            LogProcessingPaymentProcessedEvent(integrationEvent.UserId, integrationEvent.Status);

            if (integrationEvent.Status != PaymentStatus.Approved)
            {
                LogPaymentNotApproved(integrationEvent.UserId, integrationEvent.Status);
                return;
            }

            // PaymentProcessedEvent não carrega o email do usuário; reaproveitamos o
            // RecipientEmail já conhecido de uma notificação anterior (ex.: boas-vindas).
            IReadOnlyList<Notification> existingNotifications =
                await notificationRepository.GetByUserIdAsync(integrationEvent.UserId, cancellationToken);
            Notification? reference = existingNotifications.FirstOrDefault();

            if (reference is null)
            {
                LogRecipientEmailNotFound(integrationEvent.UserId);
                return;
            }

            var notification = Notification.Create(
                integrationEvent.UserId,
                NotificationType.PurchaseConfirmation,
                reference.RecipientEmail,
                reference.RecipientName,
                integrationEvent.EventId);

            await notificationRepository.AddAsync(notification, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            LogPurchaseConfirmationNotificationCreated(integrationEvent.UserId);

            await emailService.SendPurchaseConfirmationAsync(
                reference.RecipientEmail, integrationEvent.UserId, integrationEvent.GameId, cancellationToken);
        }
        catch (Exception ex)
        {
            LogErrorProcessingPaymentProcessedEvent(ex, integrationEvent.UserId, ex.Message);
            throw;
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Processando evento PaymentProcessedEvent para usuário {UserId} (status {Status})")]
    private partial void LogProcessingPaymentProcessedEvent(Guid userId, PaymentStatus status);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Pagamento não aprovado ({Status}) para usuário {UserId}, nenhum email será enviado")]
    private partial void LogPaymentNotApproved(Guid userId, PaymentStatus status);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Nenhum email conhecido para o usuário {UserId}, confirmação de compra não pôde ser enviada")]
    private partial void LogRecipientEmailNotFound(Guid userId);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Notificação de confirmação de compra criada com sucesso para usuário {UserId}")]
    private partial void LogPurchaseConfirmationNotificationCreated(Guid userId);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = "Erro ao processar PaymentProcessedEvent para usuário {UserId}: {ErrorMessage}")]
    private partial void LogErrorProcessingPaymentProcessedEvent(Exception exception, Guid userId, string errorMessage);
}
