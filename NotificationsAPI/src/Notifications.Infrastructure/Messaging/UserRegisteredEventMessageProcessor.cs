namespace Notifications.Infrastructure.Messaging;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using FiapCloudGames.Contracts.Users;
using FiapCloudGames.RabbitMq.Consumers;
using FiapCloudGames.RabbitMq.Processing;
using Microsoft.Extensions.Logging;
using Application.UseCases.Handlers;
using Domain.Shared;

/// <summary>
/// Responsável por desserializar e despachar mensagens de <see cref="UserRegisteredEvent"/>
/// para o respectivo handler, isolado da infraestrutura de conexão com o RabbitMQ (provida pelo
/// pacote FiapCloudGames.RabbitMq) e da resolução via injeção de dependência (delegada ao
/// <see cref="IEventDispatcher"/>).
/// </summary>
public partial class UserRegisteredEventMessageProcessor(
    IEventDispatcher dispatcher,
    ILogger<UserRegisteredEventMessageProcessor> logger) : IMessageProcessor
{
    /// <summary>
    /// Desserializa o corpo da mensagem e despacha para o <see cref="IEventHandler{TEvent}"/> correspondente.
    /// </summary>
    public async Task<MessageProcessingResult> ProcessAsync(
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken)
    {
        if (!TryDeserializeEvent(body, out UserRegisteredEvent? integrationEvent))
        {
            return MessageProcessingResult.PoisonMessage;
        }

        return await DispatchAsync(integrationEvent, cancellationToken);
    }

    private bool TryDeserializeEvent(ReadOnlyMemory<byte> body, [NotNullWhen(true)] out UserRegisteredEvent? integrationEvent)
    {
        try
        {
            integrationEvent = JsonSerializer.Deserialize<UserRegisteredEvent>(body.Span);
        }
        catch (JsonException ex)
        {
            LogMalformedMessage(ex);
            integrationEvent = null;
            return false;
        }

        if (integrationEvent is null)
        {
            LogEmptyMessage();
            return false;
        }

        return true;
    }

    private async Task<MessageProcessingResult> DispatchAsync(
        UserRegisteredEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            await dispatcher.DispatchAsync(integrationEvent, cancellationToken);
            return MessageProcessingResult.Success;
        }
        catch (DuplicateEventException)
        {
            // EventId já processado anteriormente: reentrega do broker, não é uma falha real.
            LogDuplicateEvent(integrationEvent.EventId, integrationEvent.UserId);
            return MessageProcessingResult.PoisonMessage;
        }
        catch (Exception ex)
        {
            LogProcessingFailed(ex, integrationEvent.UserId);
            return MessageProcessingResult.TransientFailure;
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Mensagem UserRegisteredEvent malformada recebida, descartando")]
    private partial void LogMalformedMessage(Exception exception);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Mensagem UserRegisteredEvent vazia recebida, descartando")]
    private partial void LogEmptyMessage();

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Falha ao processar UserRegisteredEvent para usuário {UserId}, mensagem será reenfileirada")]
    private partial void LogProcessingFailed(Exception exception, Guid userId);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Evento {EventId} já processado anteriormente para usuário {UserId}, descartando reentrega")]
    private partial void LogDuplicateEvent(Guid eventId, Guid userId);
}
