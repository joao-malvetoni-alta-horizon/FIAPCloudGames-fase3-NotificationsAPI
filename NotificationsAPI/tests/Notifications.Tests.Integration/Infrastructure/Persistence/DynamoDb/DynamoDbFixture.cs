using Amazon.DynamoDBv2;
using Notifications.Infrastructure.Persistence.DynamoDb;

namespace Notifications.Tests.Integration.Infrastructure.Persistence.DynamoDb;

/// <summary>
/// Fixture de classe que expõe uma <see cref="DynamoDbTable"/> aos testes do repositório.
/// </summary>
public sealed class DynamoDbFixture : IAsyncLifetime
{
    private readonly DynamoDbTable _table = new();

    public IAmazonDynamoDB Client => _table.Client;

    public DynamoDbOptions Options => _table.Options;

    public Task InitializeAsync()
    {
        return _table.StartAsync();
    }

    public Task ClearAsync()
    {
        return _table.ClearAsync();
    }

    public async Task DisposeAsync()
    {
        await _table.DisposeAsync();
    }
}
