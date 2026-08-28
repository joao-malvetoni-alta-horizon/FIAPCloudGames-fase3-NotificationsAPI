using Notifications.Domain.Notifications;
using Notifications.Domain.Shared;
using Notifications.Infrastructure.Persistence.DynamoDb;
using Shouldly;

namespace Notifications.Tests.Integration.Infrastructure.Persistence.DynamoDb;

/// <summary>
/// Testes de integração do repositório contra DynamoDB Local, cobrindo a idempotência por escrita
/// condicional (DD-04) e a consulta por usuário via <c>GSI1-UserId</c>.
/// </summary>
public class DynamoDbNotificationRepositoryTests(DynamoDbFixture fixture)
    : IClassFixture<DynamoDbFixture>, IAsyncLifetime
{
    private readonly DynamoDbNotificationRepository _repository = new(fixture.Client, fixture.Options);

    public Task InitializeAsync()
    {
        return fixture.ClearAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private static Notification CreateNotification(
        Guid? userId = null,
        Guid? eventId = null,
        string email = "user@example.com",
        string? name = "Test User")
    {
        return Notification.Create(
            userId: userId ?? Guid.NewGuid(),
            type: NotificationType.WelcomeEmail,
            recipientEmail: email,
            recipientName: name,
            eventId: eventId);
    }

    [Fact]
    public async Task AddAsync_WithValidNotification_PersistsAndRoundTripsAllFields()
    {
        var userId = Guid.NewGuid();
        Notification notification = CreateNotification(userId, Guid.NewGuid());

        await _repository.AddAsync(notification);

        IReadOnlyList<Notification> stored = await _repository.GetByUserIdAsync(userId);

        Notification retrieved = stored.ShouldHaveSingleItem();
        retrieved.Id.ShouldBe(notification.Id);
        retrieved.UserId.ShouldBe(notification.UserId);
        retrieved.Type.ShouldBe(notification.Type);
        retrieved.Status.ShouldBe(NotificationStatus.Pending);
        retrieved.RecipientEmail.ShouldBe(notification.RecipientEmail);
        retrieved.RecipientName.ShouldBe(notification.RecipientName);
        retrieved.EventId.ShouldBe(notification.EventId);
        retrieved.RetryCount.ShouldBe(notification.RetryCount);
        retrieved.CreatedAt.ShouldBe(notification.CreatedAt, TimeSpan.FromMilliseconds(1));
        retrieved.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task AddAsync_WithSameEventId_ThrowsDuplicateEventException()
    {
        // O mesmo evento reentregue pelo SQS: a segunda escrita colide na chave de partição.
        var eventId = Guid.NewGuid();
        await _repository.AddAsync(CreateNotification(eventId: eventId));

        await Should.ThrowAsync<DuplicateEventException>(
            () => _repository.AddAsync(CreateNotification(eventId: eventId)));
    }

    [Fact]
    public async Task AddAsync_WithSameEventId_PersistsOnlyOneNotification()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        await _repository.AddAsync(CreateNotification(userId, eventId));
        try
        {
            await _repository.AddAsync(CreateNotification(userId, eventId));
        }
        catch (DuplicateEventException)
        {
            // esperado: é o que o message processor trata como reentrega
        }

        IReadOnlyList<Notification> stored = await _repository.GetByUserIdAsync(userId);
        stored.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AddAsync_WithDistinctEventIds_PersistsBoth()
    {
        var userId = Guid.NewGuid();

        await _repository.AddAsync(CreateNotification(userId, Guid.NewGuid()));
        await _repository.AddAsync(CreateNotification(userId, Guid.NewGuid()));

        IReadOnlyList<Notification> stored = await _repository.GetByUserIdAsync(userId);
        stored.Count.ShouldBe(2);
    }

    [Fact]
    public async Task AddAsync_WithoutEventId_DoesNotDeduplicate()
    {
        // Sem evento de origem não há reentrega para deduplicar: a chave cai para NOTIFICATION#{Id},
        // que é único por notificação. Em produção os dois handlers sempre propagam o EventId.
        var userId = Guid.NewGuid();

        await _repository.AddAsync(CreateNotification(userId));
        await _repository.AddAsync(CreateNotification(userId));

        IReadOnlyList<Notification> stored = await _repository.GetByUserIdAsync(userId);
        stored.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNoNotifications_ReturnsEmpty()
    {
        IReadOnlyList<Notification> stored = await _repository.GetByUserIdAsync(Guid.NewGuid());

        stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyTheRequestedUser()
    {
        var userId = Guid.NewGuid();
        await _repository.AddAsync(CreateNotification(userId, Guid.NewGuid(), "alvo@example.com"));
        await _repository.AddAsync(CreateNotification(Guid.NewGuid(), Guid.NewGuid(), "outro@example.com"));

        IReadOnlyList<Notification> stored = await _repository.GetByUserIdAsync(userId);

        stored.ShouldHaveSingleItem().RecipientEmail.ShouldBe("alvo@example.com");
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsSortedByCreatedAtDescending()
    {
        var userId = Guid.NewGuid();
        Notification older = CreateNotification(userId, Guid.NewGuid(), "antiga@example.com");
        await _repository.AddAsync(older);

        // O GSI1 ordena por CreatedAt; sem folga os dois itens podem colidir no mesmo instante.
        await Task.Delay(TimeSpan.FromMilliseconds(20));

        Notification newer = CreateNotification(userId, Guid.NewGuid(), "recente@example.com");
        await _repository.AddAsync(newer);

        IReadOnlyList<Notification> stored = await _repository.GetByUserIdAsync(userId);

        stored.Count.ShouldBe(2);
        stored[0].Id.ShouldBe(newer.Id);
        stored[1].Id.ShouldBe(older.Id);
    }

    [Fact]
    public async Task AddAsync_WithNullableFieldsUnset_RoundTripsAsNull()
    {
        var userId = Guid.NewGuid();
        await _repository.AddAsync(CreateNotification(userId, Guid.NewGuid(), name: null));

        Notification retrieved = (await _repository.GetByUserIdAsync(userId)).ShouldHaveSingleItem();

        retrieved.RecipientName.ShouldBeNull();
        retrieved.LastError.ShouldBeNull();
        retrieved.Subject.ShouldBeNull();
        retrieved.Body.ShouldBeNull();
    }
}
