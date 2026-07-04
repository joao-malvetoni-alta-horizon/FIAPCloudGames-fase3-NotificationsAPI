namespace Notifications.Infrastructure.Messaging;

using FiapCloudGames.Contracts.Users;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

/// <summary>
/// Consome eventos <see cref="UserRegisteredEvent"/> publicados pelo UsersAPI na exchange
/// <see cref="UserMessaging.Exchange"/>, através de uma fila própria do NotificationsAPI.
/// </summary>
public partial class RabbitMqConsumerHostedService(
    IOptions<RabbitMqSettings> options,
    UserRegisteredEventMessageProcessor processor,
    ILogger<RabbitMqConsumerHostedService> logger) : BackgroundService
{
    private const string QueueName = "notifications.user-registered";

    private readonly RabbitMqSettings _settings = options.Value;
    private readonly TaskCompletionSource _consumerStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Concluída quando a exchange, a fila e o binding já foram declarados e o consumidor está pronto para receber mensagens.
    /// </summary>
    public Task Started => _consumerStarted.Task;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConsumerAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogConnectionLost(ex);

                try
                {
                    await Task.Delay(_settings.ConnectionRetryDelayMs, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task RunConsumerAsync(CancellationToken stoppingToken)
    {
        await using IConnection connection = await ConnectWithRetryAsync(stoppingToken);
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await DeclareMessagingInfrastructureAsync(channel, stoppingToken);
        await StartConsumerAsync(channel, stoppingToken);

        LogConsumerReady(QueueName);
        _consumerStarted.TrySetResult();

        await WaitUntilDisconnectedAsync(connection, stoppingToken);
    }

    private static async Task DeclareMessagingInfrastructureAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            UserMessaging.Exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            QueueName,
            UserMessaging.Exchange,
            UserMessaging.RoutingKeys.Registered,
            cancellationToken: cancellationToken);
    }

    private async Task StartConsumerAsync(IChannel channel, CancellationToken cancellationToken)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, args) => HandleMessageAsync(channel, args, cancellationToken);

        await channel.BasicConsumeAsync(
            QueueName,
            autoAck: false,
            consumerTag: string.Empty,
            noLocal: false,
            exclusive: false,
            arguments: null,
            consumer,
            cancellationToken: cancellationToken);
    }

    private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs args, CancellationToken cancellationToken)
    {
        try
        {
            MessageProcessingResult result = await processor.ProcessAsync(args.Body, cancellationToken);
            await AcknowledgeMessageAsync(channel, args.DeliveryTag, result, cancellationToken);
        }
        catch (Exception ex)
        {
            LogMessageHandlingFailed(ex);
        }
    }

    private static ValueTask AcknowledgeMessageAsync(
        IChannel channel,
        ulong deliveryTag,
        MessageProcessingResult result,
        CancellationToken cancellationToken)
    {
        return result switch
        {
            MessageProcessingResult.Success =>
                channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken),
            MessageProcessingResult.PoisonMessage =>
                channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken),
            MessageProcessingResult.TransientFailure =>
                channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(result))
        };
    }

    private static async Task WaitUntilDisconnectedAsync(IConnection connection, CancellationToken stoppingToken)
    {
        var connectionClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.ConnectionShutdownAsync += (_, _) =>
        {
            connectionClosed.TrySetResult();
            return Task.CompletedTask;
        };

        await using CancellationTokenRegistration registration = stoppingToken.Register(() => connectionClosed.TrySetResult());
        await connectionClosed.Task;

        stoppingToken.ThrowIfCancellationRequested();
    }

    private async Task<IConnection> ConnectWithRetryAsync(CancellationToken stoppingToken)
    {
        var factory = CreateConnectionFactory();

        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return await factory.CreateConnectionAsync(stoppingToken);
            }
            catch (Exception ex) when (attempt <= _settings.MaxConnectionRetries)
            {
                LogConnectionAttemptFailed(ex, attempt, _settings.MaxConnectionRetries);
                await Task.Delay(_settings.ConnectionRetryDelayMs, stoppingToken);
            }
        }
    }

    private ConnectionFactory CreateConnectionFactory()
    {
        return new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost
        };
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Consumidor RabbitMQ pronto, aguardando mensagens na fila {QueueName}")]
    private partial void LogConsumerReady(string queueName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Tentativa {Attempt}/{MaxAttempts} de conexão ao RabbitMQ falhou")]
    private partial void LogConnectionAttemptFailed(Exception exception, int attempt, int maxAttempts);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Conexão com o RabbitMQ perdida, tentando reconectar")]
    private partial void LogConnectionLost(Exception exception);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "Falha inesperada ao processar mensagem recebida do RabbitMQ")]
    private partial void LogMessageHandlingFailed(Exception exception);
}
