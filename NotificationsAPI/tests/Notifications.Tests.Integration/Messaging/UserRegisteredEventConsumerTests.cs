using System.Text;
using System.Text.Json;
using FiapCloudGames.Contracts.Users;
using FiapCloudGames.RabbitMq.Consumers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Domain.Notifications;
using Notifications.Infrastructure.Messaging;
using Notifications.Infrastructure.Persistence;
using NSubstitute;
using RabbitMQ.Client;
using Shouldly;

namespace Notifications.Tests.Integration.Messaging;

/// <summary>
/// Teste de integração ponta a ponta: publica um <see cref="UserRegisteredEvent"/> diretamente no
/// RabbitMQ (simulando o que o UsersAPI faz em produção) e valida que o NotificationsAPI consome a
/// mensagem, persiste a notificação e aciona o envio do email de boas-vindas.
/// </summary>
public class UserRegisteredEventConsumerTests(MessagingHostFixture fixture)
    : IClassFixture<MessagingHostFixture>, IAsyncLifetime
{
    private readonly MessagingHostFixture _fixture = fixture;

    public async Task InitializeAsync()
    {
        // O host já subiu em MessagingHostFixture.InitializeAsync; aqui só aguardamos o consumidor
        // declarar exchange/fila/binding antes de qualquer teste publicar mensagens.
        var consumer = _fixture.Services.GetRequiredService<RabbitMqConsumerHostedService<UserRegisteredEventMessageProcessor>>();
        await consumer.Started.WaitAsync(TimeSpan.FromSeconds(30));
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Consumer_WithValidEvent_PersistsNotificationAndSendsWelcomeEmail()
    {
        // Arrange
        var integrationEvent = new UserRegisteredEvent(Guid.NewGuid(), "Jane Doe", "jane@example.com")
        {
            EventId = Guid.NewGuid()
        };

        // Act
        await PublishEventAsync(integrationEvent);
        Notification? notification = await WaitForNotificationAsync(integrationEvent.EventId, TimeSpan.FromSeconds(15));

        // Assert
        notification.ShouldNotBeNull();
        notification.UserId.ShouldBe(integrationEvent.UserId);
        notification.RecipientEmail.ShouldBe(integrationEvent.Email);
        notification.Type.ShouldBe(NotificationType.WelcomeEmail);
        notification.Status.ShouldBe(NotificationStatus.Pending);

        await _fixture.EmailServiceSubstitute.Received(1)
            .SendWelcomeEmailAsync(integrationEvent.Email, integrationEvent.Name, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consumer_WithMalformedMessage_DoesNotCrashAndKeepsProcessingSubsequentMessages()
    {
        // Arrange
        byte[] malformedBody = Encoding.UTF8.GetBytes("{ isso não é um json válido");

        // Act - mensagem corrompida não deve derrubar o consumidor
        await PublishRawAsync(malformedBody);
        await Task.Delay(TimeSpan.FromSeconds(1));

        var validEvent = new UserRegisteredEvent(Guid.NewGuid(), "Resilience Test", "resilience@example.com")
        {
            EventId = Guid.NewGuid()
        };
        await PublishEventAsync(validEvent);
        Notification? notification = await WaitForNotificationAsync(validEvent.EventId, TimeSpan.FromSeconds(15));

        // Assert - consumidor continuou processando mensagens válidas normalmente
        notification.ShouldNotBeNull();
    }

    /// <summary>
    /// A constraint única de banco em Notification.EventId impede duas notificações com o mesmo
    /// EventId. Republicar o mesmo evento faz a segunda tentativa de persistência falhar por
    /// violação de unicidade; o processor trata esse conflito como mensagem descartável (não
    /// reenfileira), então o resultado observável é uma única notificação persistida.
    /// </summary>
    [Fact]
    public async Task Consumer_WithDuplicateEventId_PersistsOnlyOneNotification()
    {
        // Arrange
        var integrationEvent = new UserRegisteredEvent(Guid.NewGuid(), "Duplicate Test", "duplicate@example.com")
        {
            EventId = Guid.NewGuid()
        };

        // Act
        await PublishEventAsync(integrationEvent);
        await WaitForNotificationAsync(integrationEvent.EventId, TimeSpan.FromSeconds(15));

        await PublishEventAsync(integrationEvent);
        await Task.Delay(TimeSpan.FromSeconds(2));

        int count = await CountNotificationsByEventIdAsync(integrationEvent.EventId);

        // Assert
        count.ShouldBe(1);
    }

    private async Task<Notification?> WaitForNotificationAsync(Guid eventId, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            using IServiceScope scope = _fixture.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
            Notification? notification = await repository.GetByEventIdAsync(eventId);
            if (notification is not null)
            {
                return notification;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        return null;
    }

    private async Task<int> CountNotificationsByEventIdAsync(Guid eventId)
    {
        using IServiceScope scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.Notifications.CountAsync(n => n.EventId == eventId);
    }

    private Task PublishEventAsync(UserRegisteredEvent integrationEvent)
    {
        return PublishRawAsync(JsonSerializer.SerializeToUtf8Bytes(integrationEvent));
    }

    private async Task PublishRawAsync(byte[] body)
    {
        var connectionFactory = new ConnectionFactory
        {
            HostName = _fixture.RabbitMqContainer.Hostname,
            Port = _fixture.RabbitMqContainer.GetMappedPublicPort(5672),
            UserName = "guest",
            Password = "guest"
        };

        await using IConnection connection = await connectionFactory.CreateConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(UserMessaging.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);

        var properties = new BasicProperties { ContentType = "application/json", DeliveryMode = DeliveryModes.Persistent };
        await channel.BasicPublishAsync(UserMessaging.Exchange, UserMessaging.RoutingKeys.Registered, mandatory: false, properties, body);
    }
}
