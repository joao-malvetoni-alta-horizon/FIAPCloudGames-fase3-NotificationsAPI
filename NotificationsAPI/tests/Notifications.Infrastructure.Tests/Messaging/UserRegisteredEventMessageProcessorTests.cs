namespace Notifications.Infrastructure.Tests.Messaging;

using System.Text;
using System.Text.Json;
using FiapCloudGames.Contracts.Users;
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
/// Testes unitários para UserRegisteredEventMessageProcessor, isolando a lógica de
/// desserialização/despacho da resolução de handlers via <see cref="IEventDispatcher"/>.
/// </summary>
public class UserRegisteredEventMessageProcessorTests
{
    private readonly IEventDispatcher _dispatcher = Substitute.For<IEventDispatcher>();

    private readonly ILogger<UserRegisteredEventMessageProcessor>
        _logger = Substitute.For<ILogger<UserRegisteredEventMessageProcessor>>();

    private readonly UserRegisteredEventMessageProcessor _processor;

    public UserRegisteredEventMessageProcessorTests()
    {
        _processor = new UserRegisteredEventMessageProcessor(_dispatcher, _logger);
    }

    [Fact]
    public async Task ProcessAsync_WithValidMessage_DispatchesToHandlerAndReturnsSuccess()
    {
        // Arrange
        var integrationEvent = new UserRegisteredEvent(Guid.NewGuid(), "Jane Doe", "jane@example.com");
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent);

        // Act
        MessageProcessingResult result = await _processor.ProcessAsync(body, CancellationToken.None);

        // Assert
        result.ShouldBe(MessageProcessingResult.Success);
        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<UserRegisteredEvent>(e => e.UserId == integrationEvent.UserId),
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
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<UserRegisteredEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenDispatcherThrows_ReturnsTransientFailure()
    {
        // Arrange
        var integrationEvent = new UserRegisteredEvent(Guid.NewGuid(), "Jane Doe", "jane@example.com");
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent);

        _dispatcher.DispatchAsync(Arg.Any<UserRegisteredEvent>(), Arg.Any<CancellationToken>())
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
        var integrationEvent = new UserRegisteredEvent(Guid.NewGuid(), "Jane Doe", "jane@example.com");
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent);

        var uniqueViolation = new PostgresException("duplicate key value violates unique constraint", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation);
        _dispatcher.DispatchAsync(Arg.Any<UserRegisteredEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new DbUpdateException("duplicate", uniqueViolation)));

        // Act
        MessageProcessingResult result = await _processor.ProcessAsync(body, CancellationToken.None);

        // Assert
        result.ShouldBe(MessageProcessingResult.PoisonMessage);
    }
}
