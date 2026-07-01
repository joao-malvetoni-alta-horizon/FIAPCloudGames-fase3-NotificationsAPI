
namespace NotificationsTests.Application.UseCases.Handlers;

using Microsoft.Extensions.Logging;
using FiapCloudGames.Contracts.Users;
using NotificationsAPI.Application.UseCases.Handlers;
using NotificationsAPI.Domain.Notifications;
using NotificationsAPI.Domain.Shared;
using NSubstitute;
using Shouldly;

/// <summary>
/// Testes unitários para UserCreatedEventHandler.
/// </summary>
public class UserCreatedEventHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly ILogger<UserCreatedEventHandler> _logger = Substitute.For<ILogger<UserCreatedEventHandler>>();
    private readonly UserCreatedEventHandler _handler;

    public UserCreatedEventHandlerTests()
    {
        _unitOfWork.Notifications.Returns(_notificationRepository);
        _handler = new UserCreatedEventHandler(_unitOfWork, _logger);
    }

    [Fact]
    public async Task HandleAsync_WithValidEvent_CreatesWelcomeNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var integrationEvent = new UserCreatedEvent(userId, "John Doe", "john@example.com")
        {
            EventId = eventId
        };

        // Act
        await _handler.HandleAsync(integrationEvent);

        // Assert
        await _notificationRepository.Received(1).AddAsync(
            Arg.Is<Notification>(n =>
                n.UserId == userId &&
                n.Type == NotificationType.WelcomeEmail &&
                n.RecipientEmail == "john@example.com" &&
                n.RecipientName == "John Doe" &&
                n.EventId == eventId &&
                n.Status == NotificationStatus.Pending),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithValidEvent_CommitsChanges()
    {
        // Arrange
        var integrationEvent = new UserCreatedEvent(Guid.NewGuid(), "Jane Doe", "jane@example.com");

        // Act
        await _handler.HandleAsync(integrationEvent);

        // Assert
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithInvalidEmail_ThrowsInvalidNotificationEmailException()
    {
        // Arrange
        var integrationEvent = new UserCreatedEvent(Guid.NewGuid(), "Invalid User", "invalid-email");

        // Act & Assert
        await Should.ThrowAsync<InvalidNotificationEmailException>(
            () => _handler.HandleAsync(integrationEvent));
    }

    [Fact]
    public async Task HandleAsync_WhenCommitThrows_PropagatesException()
    {
        // Arrange
        var integrationEvent = new UserCreatedEvent(Guid.NewGuid(), "John Doe", "john@example.com");

        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new InvalidOperationException("Database error")));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _handler.HandleAsync(integrationEvent));
    }

    [Fact]
    public async Task HandleAsync_WithEmptyName_CreatesNotificationWithoutName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var integrationEvent = new UserCreatedEvent(userId, string.Empty, "user@example.com");

        // Act
        await _handler.HandleAsync(integrationEvent);

        // Assert
        await _notificationRepository.Received(1).AddAsync(
            Arg.Is<Notification>(n => n.RecipientName == string.Empty),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PreservesEventIdForIdempotency()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var integrationEvent = new UserCreatedEvent(Guid.NewGuid(), "John Doe", "john@example.com")
        {
            EventId = eventId
        };

        // Act
        await _handler.HandleAsync(integrationEvent);

        // Assert
        await _notificationRepository.Received(1).AddAsync(
            Arg.Is<Notification>(n => n.EventId == eventId),
            Arg.Any<CancellationToken>());
    }
}
