using Notifications.Domain.Notifications;
using Notifications.Domain.Shared;
using Notifications.Infrastructure.Persistence;
using Shouldly;

namespace Notifications.Tests.Integration.Infrastructure.Persistence;

/// <summary>
/// Testes de integração do UnitOfWork contra um PostgreSQL real, focados na tradução da violação
/// do índice único de <c>Notification.EventId</c> para a exceção de domínio
/// <see cref="DuplicateEventException"/>. É essa tradução que permite à camada de mensageria tratar
/// reentregas do broker sem depender de <c>DbUpdateException</c> nem de <c>PostgresException</c>.
/// </summary>
public class UnitOfWorkTests : IntegrationTestBase
{
    private static Notification CreateNotification(Guid eventId)
    {
        return Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: "user@example.com",
            recipientName: "Test User",
            eventId: eventId);
    }

    [Fact]
    public async Task CommitAsync_WithDuplicateEventId_ThrowsDuplicateEventException()
    {
        // Arrange - o mesmo EventId chegando duas vezes, como numa reentrega at-least-once
        var eventId = Guid.NewGuid();
        await using var unitOfWork = new UnitOfWork(DbContext);

        DbContext.Notifications.Add(CreateNotification(eventId));
        await unitOfWork.CommitAsync();

        DbContext.Notifications.Add(CreateNotification(eventId));

        // Act & Assert - o chamador vê uma exceção de domínio, não o erro do PostgreSQL
        var exception = await Should.ThrowAsync<DuplicateEventException>(
            () => unitOfWork.CommitAsync());

        exception.InnerException.ShouldNotBeNull();
    }

    [Fact]
    public async Task CommitAsync_WithDistinctEventIds_PersistsBoth()
    {
        // Arrange
        await using var unitOfWork = new UnitOfWork(DbContext);
        DbContext.Notifications.Add(CreateNotification(Guid.NewGuid()));
        DbContext.Notifications.Add(CreateNotification(Guid.NewGuid()));

        // Act
        int affected = await unitOfWork.CommitAsync();

        // Assert
        affected.ShouldBe(2);
    }
}
