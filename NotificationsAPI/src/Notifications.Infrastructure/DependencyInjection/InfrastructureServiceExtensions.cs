namespace Notifications.Infrastructure.DependencyInjection;

using Amazon.DynamoDBv2;
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
        services.AddSingleton(new DynamoDbOptions
        {
            TableName = configuration["DynamoDb:TableName"] ?? new DynamoDbOptions().TableName
        });

        services.AddSingleton<IAmazonDynamoDB>(_ =>
        {
            string? serviceUrl = configuration["DynamoDb:ServiceUrl"];

            return string.IsNullOrWhiteSpace(serviceUrl)
                ? new AmazonDynamoDBClient()
                : new AmazonDynamoDBClient(new AmazonDynamoDBConfig { ServiceURL = serviceUrl });
        });

        services.AddScoped<INotificationRepository, DynamoDbNotificationRepository>();
        services.AddSingleton<IEmailService, EmailService>();
        services.AddSingleton<IEventDispatcher, EventDispatcher>();

        return services;
    }
}
