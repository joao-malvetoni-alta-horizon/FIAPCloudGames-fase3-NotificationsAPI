namespace Notifications.Infrastructure.Persistence.DynamoDb;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Domain.Notifications;
using Domain.Shared;

/// <summary>
/// Repositório de notificações sobre DynamoDB.
/// </summary>
/// <remarks>
/// A escrita é imediata: <see cref="AddAsync"/> grava o item na hora, com uma escrita condicional
/// que garante a idempotência. Não há unidade de trabalho a confirmar depois — foi por isso que a
/// <c>IUnitOfWork</c> saiu do domínio junto com o EF Core.
/// </remarks>
public class DynamoDbNotificationRepository(
    IAmazonDynamoDB client,
    DynamoDbOptions options) : INotificationRepository
{
    /// <summary>
    /// Grava a notificação. A escrita é condicionada à inexistência da chave de partição, que é
    /// derivada do <c>EventId</c>: uma reentrega do mesmo evento colide e é rejeitada pelo próprio
    /// DynamoDB, sem leitura prévia nem corrida entre execuções concorrentes da função.
    /// </summary>
    /// <exception cref="DuplicateEventException">
    /// Quando o evento de origem já havia sido processado. Traduzir aqui mantém o
    /// <c>ConditionalCheckFailedException</c> da AWS contido na camada de persistência — a camada
    /// de mensageria trata a mesma exceção de domínio que tratava com o PostgreSQL.
    /// </exception>
    public async Task AddAsync(Notification entity, CancellationToken cancellationToken = default)
    {
        var request = new PutItemRequest
        {
            TableName = options.TableName,
            Item = NotificationItem.ToItem(entity),
            ConditionExpression = $"attribute_not_exists({NotificationItem.PartitionKeyAttribute})"
        };

        try
        {
            await client.PutItemAsync(request, cancellationToken);
        }
        catch (ConditionalCheckFailedException ex)
        {
            throw new DuplicateEventException(
                "O evento de integração já havia sido processado (chave de partição já existe).",
                ex);
        }
    }

    /// <summary>
    /// Consulta as notificações de um usuário pelo índice <c>GSI1-UserId</c>, da mais recente para
    /// a mais antiga. Sem esse índice a operação seria um <c>Scan</c> da tabela inteira.
    /// </summary>
    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var request = new QueryRequest
        {
            TableName = options.TableName,
            IndexName = DynamoDbOptions.UserIdIndexName,
            KeyConditionExpression = $"{NotificationItem.UserIdAttribute} = :userId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":userId"] = new(userId.ToString())
            },
            ScanIndexForward = false
        };

        QueryResponse response = await client.QueryAsync(request, cancellationToken);

        return [.. response.Items.Select(NotificationItem.FromItem)];
    }
}
