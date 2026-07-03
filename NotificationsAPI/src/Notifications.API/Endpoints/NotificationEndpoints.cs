namespace Notifications.API.Endpoints;

using NotificationsAPI.Domain.Notifications;
using NotificationsAPI.Infrastructure.Persistence;
using Notifications.API.Models;

/// <summary>
/// Endpoints para gerenciamento de notificações.
/// </summary>
public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/notifications")
            .WithName("Notifications")
            .WithOpenApi();

        group.MapGet("/", GetAllNotifications)
            .WithName("GetAllNotifications")
            .WithDescription("Lista todas as notificações");

        group.MapGet("/{id:guid}", GetNotificationById)
            .WithName("GetNotificationById")
            .WithDescription("Obtém uma notificação pelo ID");

        group.MapGet("/user/{userId:guid}", GetNotificationsByUserId)
            .WithName("GetNotificationsByUserId")
            .WithDescription("Lista as notificações de um usuário");

        group.MapGet("/status/{status}", GetNotificationsByStatus)
            .WithName("GetNotificationsByStatus")
            .WithDescription("Filtra notificações por status");

        group.MapPost("/", CreateNotification)
            .WithName("CreateNotification")
            .WithDescription("Cria uma nova notificação")
            .Accepts<CreateNotificationRequest>("application/json")
            .Produces<NotificationResponse>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}", UpdateNotification)
            .WithName("UpdateNotification")
            .WithDescription("Atualiza uma notificação")
            .Accepts<UpdateNotificationRequest>("application/json");

        group.MapDelete("/{id:guid}", DeleteNotification)
            .WithName("DeleteNotification")
            .WithDescription("Deleta uma notificação");
    }

    private static async Task<IResult> GetAllNotifications(
        NotificationRepository repository,
        CancellationToken cancellationToken)
    {
        var notifications = await repository.GetAllAsync(cancellationToken);
        var result = notifications.Select(n => new NotificationResponse(n)).ToList();
        return Results.Ok(result);
    }

    private static async Task<IResult> GetNotificationById(
        Guid id,
        NotificationRepository repository,
        CancellationToken cancellationToken)
    {
        var notification = await repository.GetByIdAsync(id, cancellationToken);
        if (notification is null)
        {
            return Results.NotFound(new { message = "Notificação não encontrada." });
        }

        return Results.Ok(new NotificationResponse(notification));
    }

    private static async Task<IResult> GetNotificationsByUserId(
        Guid userId,
        NotificationRepository repository,
        CancellationToken cancellationToken)
    {
        var notifications = await repository.GetByUserIdAsync(userId, cancellationToken);
        var result = notifications.Select(n => new NotificationResponse(n)).ToList();
        return Results.Ok(result);
    }

    private static async Task<IResult> GetNotificationsByStatus(
        string status,
        NotificationRepository repository,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<NotificationStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            return Results.BadRequest(new { message = "Status inválido." });
        }

        var notifications = await repository.GetByStatusAsync(parsedStatus, cancellationToken);
        var result = notifications.Select(n => new NotificationResponse(n)).ToList();
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateNotification(
        CreateNotificationRequest request,
        NotificationRepository repository,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var notification = Notification.Create(
            userId: request.UserId,
            type: request.Type,
            recipientEmail: request.RecipientEmail,
            recipientName: request.RecipientName,
            eventId: request.EventId);

        await repository.AddAsync(notification, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Results.Created($"/notifications/{notification.Id}", new NotificationResponse(notification));
    }

    private static async Task<IResult> UpdateNotification(
        Guid id,
        UpdateNotificationRequest request,
        NotificationRepository repository,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var notification = await repository.GetByIdAsync(id, cancellationToken);
        if (notification is null)
        {
            return Results.NotFound(new { message = "Notificação não encontrada." });
        }

        if (request.Status.HasValue && request.Status.Value != notification.Status)
        {
            switch (request.Status.Value)
            {
                case NotificationStatus.Sent:
                    notification.MarkAsSent();
                    break;
                case NotificationStatus.Delivered:
                    notification.MarkAsDelivered();
                    break;
                case NotificationStatus.Failed when request.LastError is not null:
                    notification.MarkAsFailed(request.LastError);
                    break;
            }
        }

        repository.Update(notification);
        await unitOfWork.CommitAsync(cancellationToken);

        return Results.Ok(new NotificationResponse(notification));
    }

    private static async Task<IResult> DeleteNotification(
        Guid id,
        NotificationRepository repository,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var notification = await repository.GetByIdAsync(id, cancellationToken);
        if (notification is null)
        {
            return Results.NotFound(new { message = "Notificação não encontrada." });
        }

        repository.Delete(notification);
        await unitOfWork.CommitAsync(cancellationToken);

        return Results.NoContent();
    }
}
