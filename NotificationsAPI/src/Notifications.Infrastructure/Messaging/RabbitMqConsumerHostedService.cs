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
    private readonly TaskCompletionSource _readySource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Concluída quando a exchange, a fila e o binding já foram declarados e o consumidor está pronto para receber mensagens.
    /// </summary>
    public Task Started => _readySource.Task;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndConsumeAsync(stoppingToken);
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

    private async Task ConnectAndConsumeAsync(CancellationToken stoppingToken)
    {
        await using IConnection connection = await ConnectWithRetryAsync(stoppingToken);
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            UserMessaging.Exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            QueueName,
            UserMessaging.Exchange,
            UserMessaging.RoutingKeys.Registered,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, deliverEventArgs) =>
        {
            try
            {
                MessageProcessingResult result = await processor.ProcessAsync(deliverEventArgs.Body, stoppingToken);

                switch (result)
                {
                    case MessageProcessingResult.Success:
                        await channel.BasicAckAsync(deliverEventArgs.DeliveryTag, multiple: false, stoppingToken);
                        break;
                    case MessageProcessingResult.PoisonMessage:
                        await channel.BasicNackAsync(deliverEventArgs.DeliveryTag, multiple: false, requeue: false, stoppingToken);
                        break;
                    case MessageProcessingResult.TransientFailure:
                        await channel.BasicNackAsync(deliverEventArgs.DeliveryTag, multiple: false, requeue: true, stoppingToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogMessageHandlingFailed(ex);
            }
        };

        await channel.BasicConsumeAsync(
            QueueName,
            autoAck: false,
            consumerTag: string.Empty,
            noLocal: false,
            exclusive: false,
            arguments: null,
            consumer,
            cancellationToken: stoppingToken);

        LogConsumerReady(QueueName);
        _readySource.TrySetResult();

        var connectionClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.ConnectionShutdownAsync += (_, _) =>
        {
            connectionClosed.TrySetResult();
            return Task.CompletedTask;
        };

        using CancellationTokenRegistration registration = stoppingToken.Register(() => connectionClosed.TrySetResult());
        await connectionClosed.Task;

        stoppingToken.ThrowIfCancellationRequested();
    }

    private async Task<IConnection> ConnectWithRetryAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost
        };

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
