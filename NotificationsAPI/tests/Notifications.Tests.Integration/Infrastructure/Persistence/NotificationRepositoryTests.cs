namespace NotificationsAPI.Tests.Integration.Infrastructure.Persistence;

using Notifications.Domain.Notifications;
using Notifications.Infrastructure.Persistence;
using Shouldly;
using Xunit;

/// <summary>
/// Testes de integração para o NotificationRepository usando TestContainers.
/// Testa operações de CRUD e consultas específicas contra um banco de dados PostgreSQL real.
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

        var retrieved = await repository.GetByIdAsync(notification.Id);

        retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(notification.Id);
        retrieved.RecipientEmail.ShouldBe("user@example.com");
        retrieved.RecipientName.ShouldBe("Test User");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsNotification()
    {
        var repository = GetRepository();
        var notification = Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: "user@example.com",
            recipientName: "Test User");

        await repository.AddAsync(notification);
        await DbContext.SaveChangesAsync();

        var retrieved = await repository.GetByIdAsync(notification.Id);

        retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(notification.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        var repository = GetRepository();

        var retrieved = await repository.GetByIdAsync(Guid.NewGuid());

        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleNotifications_ReturnsAll()
    {
        var repository = GetRepository();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var notification1 = Notification.Create(
            userId: userId1,
            type: NotificationType.WelcomeEmail,
            recipientEmail: "user1@example.com");

        var notification2 = Notification.Create(
            userId: userId2,
            type: NotificationType.PurchaseConfirmation,
            recipientEmail: "user2@example.com");

        await repository.AddAsync(notification1);
        await repository.AddAsync(notification2);
        await DbContext.SaveChangesAsync();

        var result = await repository.GetAllAsync();

        result.Count.ShouldBe(2);
        result.ShouldContain(n => n.Id == notification1.Id);
        result.ShouldContain(n => n.Id == notification2.Id);
    }

    [Fact]
    public async Task Update_WithModifiedNotification_UpdatesInDatabase()
    {
        var repository = GetRepository();
        var notification = Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: "user@example.com",
            recipientName: "Test User");

        await repository.AddAsync(notification);
        await DbContext.SaveChangesAsync();

        notification.MarkAsSent();
        repository.Update(notification);
        await DbContext.SaveChangesAsync();

        var retrieved = await repository.GetByIdAsync(notification.Id);

        retrieved.ShouldNotBeNull();
        retrieved.Status.ShouldBe(NotificationStatus.Sent);
    }

    [Fact]
    public async Task Delete_WithExistingNotification_RemovesFromDatabase()
    {
        var repository = GetRepository();
        var notification = Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: "user@example.com");

        await repository.AddAsync(notification);
        await DbContext.SaveChangesAsync();

        repository.Delete(notification);
        await DbContext.SaveChangesAsync();

        var retrieved = await repository.GetByIdAsync(notification.Id);

        retrieved.ShouldBeNull();
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

    [Fact]
    public async Task GetByStatusAsync_WithPendingStatus_ReturnsOnlyPendingNotifications()
    {
        var repository = GetRepository();
        var userId = Guid.NewGuid();

        var pendingNotification = Notification.Create(
            userId: userId,
            type: NotificationType.WelcomeEmail,
            recipientEmail: "user@example.com");

        var sentNotification = Notification.Create(
            userId: userId,
            type: NotificationType.PurchaseConfirmation,
            recipientEmail: "user@example.com");
        sentNotification.MarkAsSent();

        await repository.AddAsync(pendingNotification);
        await repository.AddAsync(sentNotification);
        await DbContext.SaveChangesAsync();

        var result = await repository.GetByStatusAsync(NotificationStatus.Pending);

        result.Count.ShouldBe(1);
        result.Single().Id.ShouldBe(pendingNotification.Id);
    }

    [Fact]
    public async Task GetByStatusAsync_WithNoMatchingStatus_ReturnsEmpty()
    {
        var repository = GetRepository();
        var userId = Guid.NewGuid();

        var notification = Notification.Create(
            userId: userId,
            type: NotificationType.WelcomeEmail,
            recipientEmail: "user@example.com");

        await repository.AddAsync(notification);
        await DbContext.SaveChangesAsync();

        var result = await repository.GetByStatusAsync(NotificationStatus.Sent);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByEventIdAsync_WithValidEventId_ReturnsNotification()
    {
        var repository = GetRepository();
        var eventId = Guid.NewGuid();

        var notification = Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: "user@example.com",
            eventId: eventId);

        await repository.AddAsync(notification);
        await DbContext.SaveChangesAsync();

        var retrieved = await repository.GetByEventIdAsync(eventId);

        retrieved.ShouldNotBeNull();
        retrieved.EventId.ShouldBe(eventId);
    }

    [Fact]
    public async Task GetByEventIdAsync_WithNonExistentEventId_ReturnsNull()
    {
        var repository = GetRepository();

        var retrieved = await repository.GetByEventIdAsync(Guid.NewGuid());

        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task GetByEventIdAsync_WithUniqueConstraint_ReturnsOnlyOne()
    {
        var repository = GetRepository();
        var eventId = Guid.NewGuid();

        var notification1 = Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: "user1@example.com",
            eventId: eventId);

        await repository.AddAsync(notification1);
        await DbContext.SaveChangesAsync();

        var result = await repository.GetByEventIdAsync(eventId);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(notification1.Id);
    }
}
