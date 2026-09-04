using FiapCloudGames.Contracts.Payments;
using Microsoft.Extensions.Logging;
using Notifications.Application.UseCases.Handlers;
using Notifications.Domain.Notifications;
using Notifications.Domain.Shared;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Notifications.Application.Tests.UseCases.Handlers;

/// <summary>
/// Testes unitários para PaymentProcessedEventHandler.
/// </summary>
public class PaymentProcessedEventHandlerTests
{
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();

    private readonly ILogger<PaymentProcessedEventHandler>
        _logger = Substitute.For<ILogger<PaymentProcessedEventHandler>>();

    private readonly IEmailService _emailService = Substitute.For<IEmailService>();

    private readonly PaymentProcessedEventHandler _handler;

    public PaymentProcessedEventHandlerTests()
    {
        _handler = new PaymentProcessedEventHandler(_notificationRepository, _emailService, _logger);
    }

    private void SetUpKnownRecipient(Guid userId, string email, string name)
    {
        var previousNotification = Notification.Create(userId, NotificationType.WelcomeEmail, email, name);
        _notificationRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns([previousNotification]);
    }

    [Fact]
    public async Task HandleAsync_WhenApprovedAndEmailKnown_CreatesPurchaseConfirmationNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        SetUpKnownRecipient(userId, "jane@example.com", "Jane Doe");
        var integrationEvent = new PaymentProcessedEvent(userId, gameId, PaymentStatus.Approved) { EventId = eventId };

        // Act
        await _handler.HandleAsync(integrationEvent);

        // Assert
        await _notificationRepository.Received(1).AddAsync(
            Arg.Is<Notification>(n =>
                n.UserId == userId &&
                n.Type == NotificationType.PurchaseConfirmation &&
                n.RecipientEmail == "jane@example.com" &&
                n.RecipientName == "Jane Doe" &&
                n.EventId == eventId &&
                n.Status == NotificationStatus.Pending),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenApprovedAndEmailKnown_CommitsChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetUpKnownRecipient(userId, "jane@example.com", "Jane Doe");
        var integrationEvent = new PaymentProcessedEvent(userId, Guid.NewGuid(), PaymentStatus.Approved);

        // Act
        await _handler.HandleAsync(integrationEvent);

        // Assert
    }

    [Fact]
    public async Task HandleAsync_WhenApprovedAndEmailKnown_SendsPurchaseConfirmationEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        SetUpKnownRecipient(userId, "jane@example.com", "Jane Doe");
        var integrationEvent = new PaymentProcessedEvent(userId, gameId, PaymentStatus.Approved);

        // Act
        await _handler.HandleAsync(integrationEvent);

        // Assert
        await _emailService.Received(1)
            .SendPurchaseConfirmationAsync("jane@example.com", userId, gameId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenRejected_DoesNotCreateNotificationOrSendEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetUpKnownRecipient(userId, "jane@example.com", "Jane Doe");
        var integrationEvent = new PaymentProcessedEvent(userId, Guid.NewGuid(), PaymentStatus.Rejected);

        // Act
        await _handler.HandleAsync(integrationEvent);

        // Assert
        await _notificationRepository.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _emailService.DidNotReceive().SendPurchaseConfirmationAsync(
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenApprovedButEmailUnknown_ThrowsRecipientNotReadyException()
    {
        // Arrange - usuário sem nenhuma notificação anterior nesta base (email desconhecido)
        var userId = Guid.NewGuid();
        _notificationRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Notification>());
        var integrationEvent = new PaymentProcessedEvent(userId, Guid.NewGuid(), PaymentStatus.Approved);

        // Act & Assert
        var exception = await Should.ThrowAsync<RecipientNotReadyException>(
            () => _handler.HandleAsync(integrationEvent));
        exception.UserId.ShouldBe(userId);
        await _notificationRepository.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _emailService.DidNotReceive().SendPurchaseConfirmationAsync(
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenPersistenceThrows_PropagatesException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetUpKnownRecipient(userId, "jane@example.com", "Jane Doe");
        var integrationEvent = new PaymentProcessedEvent(userId, Guid.NewGuid(), PaymentStatus.Approved);

        _notificationRepository
            .AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new TimeoutException("Connection timeout")));

        // Act & Assert
        await Should.ThrowAsync<TimeoutException>(() => _handler.HandleAsync(integrationEvent));
    }
}
