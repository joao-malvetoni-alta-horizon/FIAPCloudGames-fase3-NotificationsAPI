namespace Notifications.Infrastructure.Tests.Messaging;

using System.Text;
using System.Text.Json;
using FiapCloudGames.Contracts.Payments;
using FiapCloudGames.RabbitMq.Consumers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notifications.Application.UseCases.Handlers;
using Notifications.Infrastructure.Messaging;
using Npgsql;
using NSubstitute;
using Shouldly;
using Xunit;

/// <summary>
/// Testes unitários para PaymentProcessedEventMessageProcessor, isolando a lógica de
/// desserialização/despacho da resolução de handlers via <see cref="IEventDispatcher"/>.
/// </summary>
public class PaymentProcessedEventMessageProcessorTests
{
    private readonly IEventDispatcher _dispatcher = Substitute.For<IEventDispatcher>();

    private readonly ILogger<PaymentProcessedEventMessageProcessor>
        _logger = Substitute.For<ILogger<PaymentProcessedEventMessageProcessor>>();

    private readonly PaymentProcessedEventMessageProcessor _processor;

    public PaymentProcessedEventMessageProcessorTests()
    {
        _processor = new PaymentProcessedEventMessageProcessor(_dispatcher, _logger);
    }

    [Fact]
    public async Task ProcessAsync_WithValidMessage_DispatchesToHandlerAndReturnsSuccess()
    {
        // Arrange
        var integrationEvent = new PaymentProcessedEvent(Guid.NewGuid(), Guid.NewGuid(), PaymentStatus.Approved);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent);

        // Act
        MessageProcessingResult result = await _processor.ProcessAsync(body, CancellationToken.None);

        // Assert
        result.ShouldBe(MessageProcessingResult.Success);
        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<PaymentProcessedEvent>(e => e.UserId == integrationEvent.UserId && e.GameId == integrationEvent.GameId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithMalformedJson_ReturnsPoisonMessageAndDoesNotDispatch()
    {
        // Arrange
        byte[] body = Encoding.UTF8.GetBytes("{ isso não é json válido");

        // Act
        MessageProcessingResult result = await _processor.ProcessAsync(body, CancellationToken.None);

        // Assert
        result.ShouldBe(MessageProcessingResult.PoisonMessage);
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<PaymentProcessedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenDispatcherThrows_ReturnsTransientFailure()
    {
        // Arrange
        var integrationEvent = new PaymentProcessedEvent(Guid.NewGuid(), Guid.NewGuid(), PaymentStatus.Approved);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent);

        _dispatcher.DispatchAsync(Arg.Any<PaymentProcessedEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new TimeoutException("Connection timeout")));

        // Act
        MessageProcessingResult result = await _processor.ProcessAsync(body, CancellationToken.None);

        // Assert
        result.ShouldBe(MessageProcessingResult.TransientFailure);
    }

    [Fact]
    public async Task ProcessAsync_WhenEventIdAlreadyProcessed_ReturnsPoisonMessage()
    {
        // Arrange - reentrega do mesmo EventId viola a constraint única de Notification.EventId
        var integrationEvent = new PaymentProcessedEvent(Guid.NewGuid(), Guid.NewGuid(), PaymentStatus.Approved);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent);

        var uniqueViolation = new PostgresException("duplicate key value violates unique constraint", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation);
        _dispatcher.DispatchAsync(Arg.Any<PaymentProcessedEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new DbUpdateException("duplicate", uniqueViolation)));

        // Act
        MessageProcessingResult result = await _processor.ProcessAsync(body, CancellationToken.None);

        // Assert
        result.ShouldBe(MessageProcessingResult.PoisonMessage);
    }
}
