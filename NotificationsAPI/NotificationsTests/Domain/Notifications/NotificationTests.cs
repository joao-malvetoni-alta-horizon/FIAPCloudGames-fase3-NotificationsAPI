namespace NotificationsTests.Domain.Notifications;

using NotificationsAPI.Domain.Notifications;
using Shouldly;

/// <summary>
/// Testes unitários para o agregado Notification.
/// </summary>
public class NotificationTests
{
    private const string ValidEmail = "user@example.com";
    private const string ValidName = "John Doe";

    [Fact]
    public void Create_WithValidParameters_ReturnsNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // Act
        var notification = Notification.Create(
            userId: userId,
            type: NotificationType.WelcomeEmail,
            recipientEmail: ValidEmail,
            recipientName: ValidName,
            eventId: eventId);

        // Assert
        notification.UserId.ShouldBe(userId);
        notification.Type.ShouldBe(NotificationType.WelcomeEmail);
        notification.RecipientEmail.ShouldBe(ValidEmail);
        notification.RecipientName.ShouldBe(ValidName);
        notification.EventId.ShouldBe(eventId);
        notification.Status.ShouldBe(NotificationStatus.Pending);
        notification.RetryCount.ShouldBe(0);
        notification.LastError.ShouldBeNull();
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsNotificationDomainException()
    {
        // Arrange
        var emptyUserId = Guid.Empty;

        // Act & Assert
        Should.Throw<NotificationDomainException>(() =>
            Notification.Create(
                userId: emptyUserId,
                type: NotificationType.WelcomeEmail,
                recipientEmail: ValidEmail,
                recipientName: ValidName));
    }

    [Fact]
    public void Create_WithEmptyEmail_ThrowsNotificationDomainException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        Should.Throw<NotificationDomainException>(() =>
            Notification.Create(
                userId: userId,
                type: NotificationType.WelcomeEmail,
                recipientEmail: string.Empty,
                recipientName: ValidName));
    }

    [Fact]
    public void Create_WithWhitespaceEmail_ThrowsNotificationDomainException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        Should.Throw<NotificationDomainException>(() =>
            Notification.Create(
                userId: userId,
                type: NotificationType.WelcomeEmail,
                recipientEmail: "   ",
                recipientName: ValidName));
    }

    [Theory]
    [InlineData("invalidemail")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user @example.com")]
    public void Create_WithInvalidEmailFormat_ThrowsInvalidNotificationEmailException(string invalidEmail)
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        var exception = Should.Throw<InvalidNotificationEmailException>(() =>
            Notification.Create(
                userId: userId,
                type: NotificationType.WelcomeEmail,
                recipientEmail: invalidEmail,
                recipientName: ValidName));

        exception.Email.ShouldBe(invalidEmail);
    }

    [Fact]
    public void Create_TrimsEmailAndName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var emailWithSpaces = "  user@example.com  ";
        var nameWithSpaces = "  John Doe  ";

        // Act
        var notification = Notification.Create(
            userId: userId,
            type: NotificationType.WelcomeEmail,
            recipientEmail: emailWithSpaces,
            recipientName: nameWithSpaces);

        // Assert
        notification.RecipientEmail.ShouldBe("user@example.com");
        notification.RecipientName.ShouldBe("John Doe");
    }

    [Fact]
    public void MarkAsSent_FromPendingStatus_SucceedsAndClearsError()
    {
        // Arrange
        var notification = Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: ValidEmail,
            recipientName: ValidName);

        // Act
        notification.MarkAsSent();

        // Assert
        notification.Status.ShouldBe(NotificationStatus.Sent);
        notification.LastError.ShouldBeNull();
    }

    [Theory]
    [InlineData(NotificationStatus.Sent)]
    [InlineData(NotificationStatus.Failed)]
    [InlineData(NotificationStatus.Delivered)]
    public void MarkAsSent_FromInvalidStatus_ThrowsInvalidNotificationStatusException(NotificationStatus currentStatus)
    {
        // Arrange
        var notification = Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: ValidEmail,
            recipientName: ValidName);

        // Forçar o status (reflection para testes)
        typeof(Notification).GetProperty(nameof(Notification.Status))
            ?.SetValue(notification, currentStatus);

        // Act & Assert
        var exception = Should.Throw<InvalidNotificationStatusException>(notification.MarkAsSent);
        exception.CurrentStatus.ShouldBe(currentStatus);
        exception.ExpectedStatus.ShouldBe(NotificationStatus.Pending);
    }

    [Fact]
    public void MarkAsFailed_FromPendingStatus_SucceedsAndSetsError()
    {
        // Arrange
        var notification = Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: ValidEmail,
            recipientName: ValidName);
        var errorMessage = "SMTP connection failed";

        // Act
        notification.MarkAsFailed(errorMessage);

        // Assert
        notification.Status.ShouldBe(NotificationStatus.Failed);
        notification.LastError.ShouldBe(errorMessage);
    }

    [Fact]
    public void MarkAsDelivered_FromSentStatus_Succeeds()
    {
        // Arrange
        var notification = Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: ValidEmail,
            recipientName: ValidName);
        notification.MarkAsSent();

        // Act
        notification.MarkAsDelivered();

        // Assert
        notification.Status.ShouldBe(NotificationStatus.Delivered);
    }

    [Theory]
    [InlineData(NotificationStatus.Pending)]
    [InlineData(NotificationStatus.Failed)]
    [InlineData(NotificationStatus.Delivered)]
    public void MarkAsDelivered_FromInvalidStatus_ThrowsInvalidNotificationStatusException(
        NotificationStatus currentStatus)
    {
        // Arrange
        var notification = Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: ValidEmail,
            recipientName: ValidName);

        typeof(Notification).GetProperty(nameof(Notification.Status))
            ?.SetValue(notification, currentStatus);

        // Act & Assert
        Should.Throw<InvalidNotificationStatusException>(notification.MarkAsDelivered);
    }

    [Fact]
    public void IncrementRetryCount_IncrementsCounter()
    {
        // Arrange
        var notification = Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: ValidEmail,
            recipientName: ValidName);

        // Act
        notification.IncrementRetryCount();
        notification.IncrementRetryCount();

        // Assert
        notification.RetryCount.ShouldBe(2);
    }

    [Fact]
    public void ResetForRetry_BelowMaxAttempts_ResetsStatusAndUpdatesTimestamp()
    {
        // Arrange
        var notification = Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: ValidEmail,
            recipientName: ValidName);
        notification.MarkAsFailed("Error");
        notification.IncrementRetryCount();

        // Act
        notification.ResetForRetry();

        // Assert
        notification.Status.ShouldBe(NotificationStatus.Pending);
    }

    [Fact]
    public void ResetForRetry_AtMaxAttempts_ThrowsMaxRetryAttemptsExceededException()
    {
        // Arrange
        var notification = Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: ValidEmail,
            recipientName: ValidName);

        // Simular 3 tentativas
        for (int i = 0; i < MaxRetryAttemptsExceededException.MaxAttempts; i++)
        {
            notification.IncrementRetryCount();
        }

        // Act & Assert
        var exception = Should.Throw<MaxRetryAttemptsExceededException>(notification.ResetForRetry);
        exception.CurrentRetryCount.ShouldBe(MaxRetryAttemptsExceededException.MaxAttempts);
    }

    [Fact]
    public void Notification_WithoutEventId_IsOptional()
    {
        // Arrange & Act
        var notification = Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: ValidEmail,
            recipientName: ValidName);

        // Assert
        notification.EventId.ShouldBeNull();
    }

    [Fact]
    public void Notification_WithoutRecipientName_IsOptional()
    {
        // Arrange & Act
        var notification = Notification.Create(
            userId: Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: ValidEmail,
            recipientName: null);

        // Assert
        notification.RecipientName.ShouldBeNull();
    }
}
