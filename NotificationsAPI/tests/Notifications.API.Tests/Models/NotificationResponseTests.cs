using Notifications.API.Models;
using Notifications.Domain.Notifications;
using Shouldly;
using Xunit;

namespace Notifications.API.Tests.Models;

public class NotificationResponseTests
{
    [Fact]
    public void FromDomain_WithNotification_MapsAllPropertiesCorrectly()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var notification = Notification.Create(
            userId: userId,
            type: NotificationType.WelcomeEmail,
            recipientEmail: "user@example.com",
            recipientName: "Test User",
            eventId: eventId);

        var response = NotificationResponse.FromDomain(notification);

        response.Id.ShouldBe(notification.Id);
        response.UserId.ShouldBe(userId);
        response.Type.ShouldBe(NotificationType.WelcomeEmail);
        response.Status.ShouldBe(NotificationStatus.Pending);
        response.RecipientEmail.ShouldBe("user@example.com");
        response.RecipientName.ShouldBe("Test User");
        response.EventId.ShouldBe(eventId);
        response.RetryCount.ShouldBe(0);
        response.LastError.ShouldBeNull();
        response.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void FromDomain_WithFailedNotification_MapsLastErrorAndStatus()
    {
        var notification = Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.PurchaseConfirmation,
            recipientEmail: "user@example.com");

        notification.MarkAsFailed("SMTP timeout");

        var response = NotificationResponse.FromDomain(notification);

        response.Status.ShouldBe(NotificationStatus.Failed);
        response.LastError.ShouldBe("SMTP timeout");
        response.UpdatedAt.ShouldNotBeNull();
    }
}
