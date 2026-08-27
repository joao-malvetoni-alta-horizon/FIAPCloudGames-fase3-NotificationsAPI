namespace Notifications.Infrastructure.Tests.Messaging;

using FiapCloudGames.Contracts.Users;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.UseCases.Handlers;
using Notifications.Infrastructure.Messaging;
using NSubstitute;
using Shouldly;
using Xunit;

/// <summary>
/// Testes unitários para EventDispatcher, o único ponto do projeto que resolve
/// <see cref="IEventHandler{TEvent}"/> a partir do contêiner de injeção de dependência.
/// </summary>
public class EventDispatcherTests
{
    private readonly IEventHandler<UserRegisteredEvent> _eventHandler = Substitute.For<IEventHandler<UserRegisteredEvent>>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly EventDispatcher _dispatcher;

    public EventDispatcherTests()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IEventHandler<UserRegisteredEvent>)).Returns(_eventHandler);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        _scopeFactory.CreateScope().Returns(scope);

        _dispatcher = new EventDispatcher(_scopeFactory);
    }

    [Fact]
    public async Task DispatchAsync_ResolvesHandlerFromScopeAndInvokesIt()
    {
        // Arrange
        var integrationEvent = new UserRegisteredEvent(Guid.NewGuid(), "Jane Doe", "jane@example.com");

        // Act
        await _dispatcher.DispatchAsync(integrationEvent, CancellationToken.None);

        // Assert
        await _eventHandler.Received(1).HandleAsync(integrationEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerThrows_PropagatesException()
    {
        // Arrange
        var integrationEvent = new UserRegisteredEvent(Guid.NewGuid(), "Jane Doe", "jane@example.com");
        _eventHandler.HandleAsync(integrationEvent, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new TimeoutException("Connection timeout")));

        // Act & Assert
        await Should.ThrowAsync<TimeoutException>(() => _dispatcher.DispatchAsync(integrationEvent, CancellationToken.None));
    }
}
