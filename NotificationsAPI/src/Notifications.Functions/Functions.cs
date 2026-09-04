using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Trace;
using Notifications.Application.UseCases.Handlers;
using FiapCloudGames.Contracts.Users;
using FiapCloudGames.Contracts.Payments;
using Notifications.Functions.Processing;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Notifications.Functions;

public class Functions
{
    private static readonly IServiceProvider ServiceProvider = Startup.BuildServiceProvider();
    private static readonly TracerProvider TracerProvider = Telemetry.BuildTracerProvider("fcg-notifications-lambda");

    /// <summary>Handler exposto ao Lambda. Configurado no template.yaml como gatilho da fila de cadastro.</summary>
    public Task<SQSBatchResponse> UserRegisteredHandler(SQSEvent sqsEvent, ILambdaContext context)
        => AWSLambdaWrapper.TraceAsync(TracerProvider, UserRegisteredHandlerInternal, sqsEvent, context);

    private async Task<SQSBatchResponse> UserRegisteredHandlerInternal(SQSEvent sqsEvent, ILambdaContext context)
    {
        await using AsyncServiceScope scope = ServiceProvider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Functions>>();

        return await SqsBatchProcessor.ProcessAsync<UserRegisteredEvent>(
            sqsEvent,
            (evt, ct) => dispatcher.DispatchAsync(evt, ct),
            logger,
            CancellationToken.None);
    }

    /// <summary>Handler exposto ao Lambda. Configurado no template.yaml como gatilho da fila de pagamento.</summary>
    public Task<SQSBatchResponse> PaymentProcessedHandler(SQSEvent sqsEvent, ILambdaContext context)
        => AWSLambdaWrapper.TraceAsync(TracerProvider, PaymentProcessedHandlerInternal, sqsEvent, context);

    private async Task<SQSBatchResponse> PaymentProcessedHandlerInternal(SQSEvent sqsEvent, ILambdaContext context)
    {
        await using AsyncServiceScope scope = ServiceProvider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Functions>>();

        return await SqsBatchProcessor.ProcessAsync<PaymentProcessedEvent>(
            sqsEvent,
            (evt, ct) => dispatcher.DispatchAsync(evt, ct),
            logger,
            CancellationToken.None);
    }
}
