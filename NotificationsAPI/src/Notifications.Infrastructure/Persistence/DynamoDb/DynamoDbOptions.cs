namespace Notifications.Infrastructure.Persistence.DynamoDb;

/// <summary>
/// Configuração de acesso à tabela de notificações.
/// </summary>
public class DynamoDbOptions
{
    /// <summary>Nome do índice secundário que suporta a consulta por usuário.</summary>
    public const string UserIdIndexName = "GSI1-UserId";

    /// <summary>Nome da tabela. Provisionada pelo template.yaml (SAM).</summary>
    public string TableName { get; set; } = "fcg-notifications";
}
