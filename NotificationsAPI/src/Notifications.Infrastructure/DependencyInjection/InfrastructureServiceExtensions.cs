namespace Notifications.Infrastructure.DependencyInjection;

using Amazon.DynamoDBv2;
using FiapCloudGames.Contracts.Payments;
using FiapCloudGames.Contracts.Users;
using FiapCloudGames.RabbitMq.Consumers;
using FiapCloudGames.RabbitMq.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.UseCases.Handlers;
using Domain.Notifications;
using Email;
using Messaging;
using Persistence.DynamoDb;

/// <summary>
/// Métodos de extensão para registrar serviços de infraestrutura.
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Adiciona serviços de infraestrutura ao contêiner de injeção de dependência.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Persistência em DynamoDB. A tabela e o índice são provisionados pelo template.yaml;
        // não há migração a rodar no startup, o que também elimina a corrida entre execuções
        // concorrentes da função durante o cold start.
        services.AddSingleton(new DynamoDbOptions
        {
            TableName = configuration["DynamoDb:TableName"] ?? new DynamoDbOptions().TableName
        });

        services.AddSingleton<IAmazonDynamoDB>(_ =>
        {
            string? serviceUrl = configuration["DynamoDb:ServiceUrl"];

            // ServiceUrl só é definido em desenvolvimento e testes, apontando para o DynamoDB
            // Local. Na AWS o cliente resolve região e credenciais pelo ambiente da Lambda.
            return string.IsNullOrWhiteSpace(serviceUrl)
                ? new AmazonDynamoDBClient()
                : new AmazonDynamoDBClient(new AmazonDynamoDBConfig { ServiceURL = serviceUrl });
        });

        services.AddScoped<INotificationRepository, DynamoDbNotificationRepository>();

        // Registrar serviço de email
        services.AddSingleton<IEmailService, EmailService>();
        services.AddSingleton<IEventDispatcher, EventDispatcher>();

        // Registrar infraestrutura RabbitMQ (FiapCloudGames.RabbitMq) e os consumidores de eventos
        services.AddRabbitMq(configuration);
        services.AddRabbitMqConsumer<UserRegisteredEventMessageProcessor>(
            new RabbitMqConsumerDefinition(
                UserMessaging.Exchange,
                "notifications.user-registered",
                UserMessaging.RoutingKeys.Registered));
        services.AddRabbitMqConsumer<PaymentProcessedEventMessageProcessor>(
            new RabbitMqConsumerDefinition(
                PaymentsMessaging.Exchange,
                "notifications.payment-processed",
                PaymentsMessaging.RoutingKeys.Status));

        return services;
    }
}
