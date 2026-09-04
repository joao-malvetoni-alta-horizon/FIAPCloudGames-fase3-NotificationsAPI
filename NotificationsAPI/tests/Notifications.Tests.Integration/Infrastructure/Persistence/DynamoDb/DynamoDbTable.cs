using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Notifications.Infrastructure.Persistence.DynamoDb;
using Testcontainers.DynamoDb;

namespace Notifications.Tests.Integration.Infrastructure.Persistence.DynamoDb;

/// <summary>
/// Sobe o DynamoDB Local em container e cria a tabela de notificações.
/// </summary>
/// <remarks>
/// O schema aqui espelha o que o <c>template.yaml</c> vai provisionar — chave <c>PK</c>,
/// <c>PAY_PER_REQUEST</c> e o índice <c>GSI1-UserId</c>. Fica num único lugar para que os testes
/// do repositório e os de mensageria não divirjam na definição da tabela.
/// </remarks>
public sealed class DynamoDbTable : IAsyncDisposable
{
    // -sharedDb é obrigatório aqui. Sem ele o DynamoDB Local mantém um banco separado por
    // (access key, região), e o cliente montado pelo DI — que resolve credenciais do ambiente —
    // não enxergaria a tabela criada por este fixture.
    private readonly DynamoDbContainer _container = new DynamoDbBuilder()
        .WithImage("amazon/dynamodb-local:latest")
        .WithCommand("-jar", "DynamoDBLocal.jar", "-inMemory", "-sharedDb")
        .WithCleanUp(true)
        .Build();

    private AmazonDynamoDBClient? _client;

    public DynamoDbOptions Options { get; } = new() { TableName = "fcg-notifications-test" };

    public string ServiceUrl => _container.GetConnectionString();

    public IAmazonDynamoDB Client => _client
        ?? throw new InvalidOperationException("O container ainda não foi iniciado.");

    public async Task StartAsync()
    {
        await _container.StartAsync();

        _client = new AmazonDynamoDBClient(
            new BasicAWSCredentials("test", "test"),
            new AmazonDynamoDBConfig
            {
                ServiceURL = ServiceUrl,
                AuthenticationRegion = RegionEndpoint.USEast1.SystemName
            });

        await CreateTableAsync();
    }

    /// <summary>Apaga todos os itens, para isolar um teste do outro.</summary>
    public async Task ClearAsync()
    {
        ScanResponse scan = await Client.ScanAsync(new ScanRequest(Options.TableName));

        foreach (Dictionary<string, AttributeValue> item in scan.Items)
        {
            await Client.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = Options.TableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    [NotificationItem.PartitionKeyAttribute] = item[NotificationItem.PartitionKeyAttribute]
                }
            });
        }
    }

    private async Task CreateTableAsync()
    {
        await Client.CreateTableAsync(new CreateTableRequest
        {
            TableName = Options.TableName,
            BillingMode = BillingMode.PAY_PER_REQUEST,
            AttributeDefinitions =
            [
                new AttributeDefinition(NotificationItem.PartitionKeyAttribute, ScalarAttributeType.S),
                new AttributeDefinition(NotificationItem.UserIdAttribute, ScalarAttributeType.S),
                new AttributeDefinition(NotificationItem.CreatedAtAttribute, ScalarAttributeType.S)
            ],
            KeySchema =
            [
                new KeySchemaElement(NotificationItem.PartitionKeyAttribute, KeyType.HASH)
            ],
            GlobalSecondaryIndexes =
            [
                new GlobalSecondaryIndex
                {
                    IndexName = DynamoDbOptions.UserIdIndexName,
                    KeySchema =
                    [
                        new KeySchemaElement(NotificationItem.UserIdAttribute, KeyType.HASH),
                        new KeySchemaElement(NotificationItem.CreatedAtAttribute, KeyType.RANGE)
                    ],
                    Projection = new Projection { ProjectionType = ProjectionType.ALL }
                }
            ]
        });
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        await _container.StopAsync();
        await _container.DisposeAsync();
    }
}
