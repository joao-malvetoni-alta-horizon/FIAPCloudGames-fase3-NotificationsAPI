namespace Notifications.Infrastructure.Persistence.DynamoDb;

using System.Globalization;
using System.Reflection;
using Amazon.DynamoDBv2.Model;
using Domain.Notifications;

/// <summary>
/// Tradução entre o agregado <see cref="Notification"/> e o item do DynamoDB.
/// </summary>
/// <remarks>
/// <para>
/// A chave de partição é derivada do <c>EventId</c> quando ele existe:
/// <c>EVENT#{EventId}</c>. É isso que torna a idempotência possível — no DynamoDB uma
/// <c>ConditionExpression</c> é avaliada contra o item que existe naquela chave primária, nunca
/// contra um atributo comum. Condicionar em <c>attribute_not_exists(EventId)</c> com uma chave
/// derivada do <c>Id</c> (um Guid novo a cada notificação) não rejeitaria nada, porque nunca há
/// item naquela chave.
/// </para>
/// <para>
/// Sem <c>EventId</c> a chave cai para <c>NOTIFICATION#{Id}</c>. Nesse caso não há idempotência,
/// o que é a semântica correta: sem evento de origem, não há reentrega para deduplicar. Em
/// produção os dois handlers sempre propagam o <c>EventId</c> do evento de integração.
/// </para>
/// <para>
/// A hidratação usa reflexão porque o agregado tem construtor e setters privados — a mesma
/// abordagem que o EF Core usava para materializar a entidade. O domínio segue inalterado.
/// </para>
/// </remarks>
public static class NotificationItem
{
    public const string PartitionKeyAttribute = "PK";
    public const string UserIdAttribute = "UserId";
    public const string CreatedAtAttribute = "CreatedAt";

    /// <summary>Monta a chave de partição de uma notificação.</summary>
    public static string BuildPartitionKey(Notification notification)
    {
        return notification.EventId is { } eventId
            ? $"EVENT#{eventId}"
            : $"NOTIFICATION#{notification.Id}";
    }

    /// <summary>Converte o agregado no item a ser gravado.</summary>
    public static Dictionary<string, AttributeValue> ToItem(Notification notification)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            [PartitionKeyAttribute] = new(BuildPartitionKey(notification)),
            ["Id"] = new(notification.Id.ToString()),
            [UserIdAttribute] = new(notification.UserId.ToString()),
            ["Type"] = new(notification.Type.ToString()),
            ["Status"] = new(notification.Status.ToString()),
            ["RecipientEmail"] = new(notification.RecipientEmail),
            ["RetryCount"] = new() { N = notification.RetryCount.ToString(CultureInfo.InvariantCulture) },
            [CreatedAtAttribute] = new(ToIso(notification.CreatedAt))
        };

        AddIfPresent(item, "RecipientName", notification.RecipientName);
        AddIfPresent(item, "Subject", notification.Subject);
        AddIfPresent(item, "Body", notification.Body);
        AddIfPresent(item, "LastError", notification.LastError);
        AddIfPresent(item, "EventId", notification.EventId?.ToString());

        if (notification.UpdatedAt is { } updatedAt)
        {
            item["UpdatedAt"] = new(ToIso(updatedAt));
        }

        return item;
    }

    /// <summary>Reconstrói o agregado a partir de um item lido da tabela.</summary>
    public static Notification FromItem(Dictionary<string, AttributeValue> item)
    {
        var notification = (Notification)Activator.CreateInstance(typeof(Notification), nonPublic: true)!;

        Set(notification, nameof(Notification.Id), Guid.Parse(item["Id"].S));
        Set(notification, nameof(Notification.UserId), Guid.Parse(item[UserIdAttribute].S));
        Set(notification, nameof(Notification.Type), Enum.Parse<NotificationType>(item["Type"].S));
        Set(notification, nameof(Notification.Status), Enum.Parse<NotificationStatus>(item["Status"].S));
        Set(notification, nameof(Notification.RecipientEmail), item["RecipientEmail"].S);
        Set(notification, nameof(Notification.RetryCount), int.Parse(item["RetryCount"].N, CultureInfo.InvariantCulture));
        Set(notification, nameof(Notification.CreatedAt), FromIso(item[CreatedAtAttribute].S));

        Set(notification, nameof(Notification.RecipientName), ReadOptionalString(item, "RecipientName"));
        Set(notification, nameof(Notification.Subject), ReadOptionalString(item, "Subject"));
        Set(notification, nameof(Notification.Body), ReadOptionalString(item, "Body"));
        Set(notification, nameof(Notification.LastError), ReadOptionalString(item, "LastError"));

        if (ReadOptionalString(item, "EventId") is { } eventId)
        {
            Set(notification, nameof(Notification.EventId), Guid.Parse(eventId));
        }

        if (ReadOptionalString(item, "UpdatedAt") is { } updatedAt)
        {
            Set(notification, nameof(Notification.UpdatedAt), FromIso(updatedAt));
        }

        return notification;
    }

    private static void AddIfPresent(Dictionary<string, AttributeValue> item, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            item[name] = new AttributeValue(value);
        }
    }

    private static string? ReadOptionalString(Dictionary<string, AttributeValue> item, string name)
    {
        return item.TryGetValue(name, out AttributeValue? value) && !string.IsNullOrEmpty(value.S)
            ? value.S
            : null;
    }

    private static void Set(Notification notification, string propertyName, object? value)
    {
        PropertyInfo property = typeof(Notification).GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Propriedade {propertyName} não encontrada em Notification.");

        (property.GetSetMethod(nonPublic: true)
            ?? throw new InvalidOperationException($"Propriedade {propertyName} não tem setter."))
            .Invoke(notification, [value]);
    }

    private static string ToIso(DateTime value)
    {
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static DateTime FromIso(string value)
    {
        return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();
    }
}
