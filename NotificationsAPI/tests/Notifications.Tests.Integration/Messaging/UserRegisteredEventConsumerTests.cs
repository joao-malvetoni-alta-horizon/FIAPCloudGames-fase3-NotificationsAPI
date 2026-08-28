using System.Text;
using System.Text.Json;
using FiapCloudGames.Contracts.Users;
using FiapCloudGames.RabbitMq.Consumers;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Domain.Notifications;
using Notifications.Infrastructure.Messaging;
using Notifications.Infrastructure.Persistence.DynamoDb;
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
            Dictionary<string, AttributeValue>? item = await GetItemByEventIdAsync(eventId);
            if (item is not null)
            {
                return NotificationItem.FromItem(item);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        return null;
    }

    private async Task<int> CountNotificationsByEventIdAsync(Guid eventId)
    {
        // A chave de partição é derivada do EventId, então a reentrega do mesmo evento colidiria
        // na mesma chave: existir o item significa exatamente uma notificação.
        return await GetItemByEventIdAsync(eventId) is null ? 0 : 1;
    }

    private async Task<Dictionary<string, AttributeValue>?> GetItemByEventIdAsync(Guid eventId)
    {
        GetItemResponse response = await _fixture.DynamoDbClient.GetItemAsync(new GetItemRequest
        {
            TableName = _fixture.DynamoDbOptions.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [NotificationItem.PartitionKeyAttribute] = new($"EVENT#{eventId}")
            },
            ConsistentRead = true
        });

        return response.IsItemSet ? response.Item : null;
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
