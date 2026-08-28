using Microsoft.EntityFrameworkCore;
using Notifications.Domain.Notifications;
using Notifications.Infrastructure.Persistence;
using Shouldly;

namespace Notifications.Tests.Integration.Infrastructure.Persistence;

/// <summary>
/// Testes de integração para o NotificationRepository usando TestContainers, contra um banco de
/// dados PostgreSQL real. Cobrem apenas os membros que o repositório ainda expõe: AddAsync e
/// GetByUserIdAsync. A verificação da escrita é feita direto pelo DbContext, e não por um
/// GetByIdAsync que só existiria para servir ao próprio teste.
/// </summary>
public class NotificationRepositoryTests : IntegrationTestBase
{
    private NotificationRepository GetRepository()
    {
        return new NotificationRepository(DbContext);
    }

    [Fact]
    public async Task AddAsync_WithValidNotification_PersistsInDatabase()
    {
        var repository = GetRepository();
        var notification = Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: "user@example.com",
            recipientName: "Test User");

        await repository.AddAsync(notification);
        await DbContext.SaveChangesAsync();

        Notification? retrieved = await DbContext.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notification.Id);

        retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(notification.Id);
        retrieved.RecipientEmail.ShouldBe("user@example.com");
        retrieved.RecipientName.ShouldBe("Test User");
    }

    [Fact]
    public async Task GetByUserIdAsync_WithValidUserId_ReturnsUserNotifications()
    {
        var repository = GetRepository();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var notification1 = Notification.Create(
            userId: userId,
            type: NotificationType.WelcomeEmail,
            recipientEmail: "user@example.com");

        var notification2 = Notification.Create(
            userId: userId,
            type: NotificationType.PurchaseConfirmation,
            recipientEmail: "user@example.com");

        var notificationOtherUser = Notification.Create(
            userId: otherUserId,
            type: NotificationType.WelcomeEmail,
            recipientEmail: "other@example.com");

        await repository.AddAsync(notification1);
        await repository.AddAsync(notification2);
        await repository.AddAsync(notificationOtherUser);
        await DbContext.SaveChangesAsync();

        var result = await repository.GetByUserIdAsync(userId);

        result.Count.ShouldBe(2);
        result.ShouldAllBe(n => n.UserId == userId);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNonExistentUserId_ReturnsEmpty()
    {
        var repository = GetRepository();

        var result = await repository.GetByUserIdAsync(Guid.NewGuid());

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsSortedByCreatedAtDescending()
    {
        var repository = GetRepository();
        var userId = Guid.NewGuid();

        var notification1 = Notification.Create(
            userId: userId,
            type: NotificationType.WelcomeEmail,
            recipientEmail: "user@example.com");

        await repository.AddAsync(notification1);
        await DbContext.SaveChangesAsync();

        await Task.Delay(100);

        var notification2 = Notification.Create(
            userId: userId,
            type: NotificationType.PurchaseConfirmation,
            recipientEmail: "user@example.com");

        await repository.AddAsync(notification2);
        await DbContext.SaveChangesAsync();

        var result = await repository.GetByUserIdAsync(userId);

        result.First().Id.ShouldBe(notification2.Id);
        result.Last().Id.ShouldBe(notification1.Id);
    }

}
