using FiapCloudGames.Contracts.Users;
using Microsoft.Extensions.Logging;
using Notifications.Application.UseCases.Handlers;
using Notifications.Domain.Notifications;
using Notifications.Domain.Shared;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Notifications.Application.Tests.UseCases.Handlers;

/// <summary>
/// Testes unitários para UserRegisteredEventHandler.
/// </summary>
public class UserRegisteredEventHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();

    private readonly ILogger<UserRegisteredEventHandler>
        _logger = Substitute.For<ILogger<UserRegisteredEventHandler>>();

    private readonly IEmailService _emailService = Substitute.For<IEmailService>();

    private readonly UserRegisteredEventHandler _handler;

    public UserRegisteredEventHandlerTests()
    {
        _handler = new UserRegisteredEventHandler(_notificationRepository, _unitOfWork, _emailService, _logger);
    }

    [Fact]
    public async Task HandleAsync_WithValidEvent_CreatesWelcomeNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var integrationEvent = new UserRegisteredEvent(userId, "Jane Doe", "jane@example.com")
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
                n.RecipientEmail == "jane@example.com" &&
                n.RecipientName == "Jane Doe" &&
                n.EventId == eventId &&
                n.Status == NotificationStatus.Pending),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithValidEvent_CommitsChanges()
    {
        // Arrange
        var integrationEvent = new UserRegisteredEvent(Guid.NewGuid(), "Self Registered User", "self@example.com");

        // Act
        await _handler.HandleAsync(integrationEvent);

        // Assert
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithInvalidEmail_ThrowsInvalidNotificationEmailException()
    {
        // Arrange
        var integrationEvent = new UserRegisteredEvent(Guid.NewGuid(), "Invalid User", "not-an-email");

        // Act & Assert
        await Should.ThrowAsync<InvalidNotificationEmailException>(() => _handler.HandleAsync(integrationEvent));
    }

    [Fact]
    public async Task HandleAsync_WhenCommitThrows_PropagatesException()
    {
        // Arrange
        var integrationEvent = new UserRegisteredEvent(Guid.NewGuid(), "Jane Doe", "jane@example.com");

        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new TimeoutException("Connection timeout")));

        // Act & Assert
        await Should.ThrowAsync<TimeoutException>(() => _handler.HandleAsync(integrationEvent));
    }

    [Fact]
    public async Task HandleAsync_WithEmailWhitespace_TrimsEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var integrationEvent = new UserRegisteredEvent(userId, "Jane Doe", "  jane@example.com  ");

        // Act
        await _handler.HandleAsync(integrationEvent);

        // Assert
        await _notificationRepository.Received(1).AddAsync(
            Arg.Is<Notification>(n => n.RecipientEmail == "jane@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PreservesEventIdForIdempotency()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var integrationEvent = new UserRegisteredEvent(Guid.NewGuid(), "Jane Doe", "jane@example.com")
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

    [Fact]
    public async Task HandleAsync_MultipleCallsWithSameEvent_AreIdempotent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var integrationEvent = new UserRegisteredEvent(Guid.NewGuid(), "Jane Doe", "jane@example.com")
        {
            EventId = eventId
        };

        // Act
        await _handler.HandleAsync(integrationEvent);
        await _handler.HandleAsync(integrationEvent);

        // Assert
        await _notificationRepository.Received(2).AddAsync(
            Arg.Is<Notification>(n => n.EventId == eventId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithValidEvent_SendsWelcomeEmail()
    {
        // Arrange
        var integrationEvent = new UserRegisteredEvent(Guid.NewGuid(), "Jane Doe", "jane@example.com");

        // Act
        await _handler.HandleAsync(integrationEvent);

        // Assert
        await _emailService.Received(1)
            .SendWelcomeEmailAsync("jane@example.com", "Jane Doe", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var integrationEvent = new UserRegisteredEvent(Guid.NewGuid(), "Jane Doe", "jane@example.com");

        // Act
        await _handler.HandleAsync(integrationEvent, cancellationToken);

        // Assert
        await _notificationRepository.Received(1).AddAsync(
            Arg.Any<Notification>(),
            cancellationToken);
    }
}
