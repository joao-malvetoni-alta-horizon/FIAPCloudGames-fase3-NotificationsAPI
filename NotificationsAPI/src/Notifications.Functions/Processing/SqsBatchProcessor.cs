namespace Notifications.Functions.Processing;

using System.Text.Json;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.Logging;
using Domain.Shared;

/// <summary>
/// Processa um lote de mensagens SQS, isolando o handler de negócio dos detalhes de
/// desserialização e de como cada tipo de falha deve ser reportado ao SQS.
/// </summary>
/// <remarks>
/// Mapeamento de resultado (equivalente ao <c>MessageProcessingResult</c> da era RabbitMQ):
/// <list type="bullet">
/// <item>Sucesso → mensagem não entra na resposta de falhas; o SQS a remove da fila.</item>
/// <item><see cref="DuplicateEventException"/> ou <see cref="JsonException"/> → mensagem
/// envenenada ou duplicada; logamos e NÃO reportamos como falha, para o SQS não reentregar.</item>
/// <item>Qualquer outra exceção (incluindo <see cref="RecipientNotReadyException"/>) → reportamos
/// o <c>itemIdentifier</c> como falha, o SQS reentrega com backoff, e após esgotar
/// <c>maxReceiveCount</c> a mensagem cai na DLQ.</item>
/// </list>
/// </remarks>
public static class SqsBatchProcessor
{
    public static async Task<SQSBatchResponse> ProcessAsync<TEvent>(
        SQSEvent sqsEvent,
        Func<TEvent, CancellationToken, Task> dispatch,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var failures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (SQSEvent.SQSMessage record in sqsEvent.Records)
        {
            try
            {
                TEvent? integrationEvent = JsonSerializer.Deserialize<TEvent>(record.Body);

                if (integrationEvent is null)
                {
                    logger.LogWarning(
                        "Mensagem {MessageId} desserializou para null, descartando", record.MessageId);
                    continue;
                }

                await dispatch(integrationEvent, cancellationToken);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(
                    ex, "Mensagem {MessageId} malformada, descartando", record.MessageId);
            }
            catch (DuplicateEventException ex)
            {
                logger.LogWarning(
                    ex,
                    "Mensagem {MessageId} já processada anteriormente, descartando reentrega",
                    record.MessageId);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Falha ao processar mensagem {MessageId}, será reenfileirada",
                    record.MessageId);
                failures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = record.MessageId });
            }
        }

        return new SQSBatchResponse(failures);
    }
}
