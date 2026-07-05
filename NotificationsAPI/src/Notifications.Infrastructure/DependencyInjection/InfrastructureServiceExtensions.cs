namespace Notifications.Infrastructure.DependencyInjection;

using FiapCloudGames.Contracts.Users;
using FiapCloudGames.RabbitMq.Consumers;
using FiapCloudGames.RabbitMq.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.UseCases.Handlers;
using Domain.Notifications;
using Domain.Shared;
using Email;
using Messaging;
using Persistence;

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
        // Registrar contexto de banco de dados
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var currentConfiguration = serviceProvider.GetRequiredService<IConfiguration>();
            options.UseNpgsql(
                currentConfiguration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
        });

        // Registrar Unit of Work e repositórios
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Registrar serviço de email
        services.AddSingleton<IEmailService, EmailService>();
        services.AddSingleton<IEventDispatcher, EventDispatcher>();

        // Registrar infraestrutura RabbitMQ (FiapCloudGames.RabbitMq) e o consumidor de UserRegisteredEvent
        services.AddRabbitMq(configuration);
        services.AddRabbitMqConsumer<UserRegisteredEventMessageProcessor>(
            new RabbitMqConsumerDefinition(
                UserMessaging.Exchange,
                "notifications.user-registered",
                UserMessaging.RoutingKeys.Registered));

        return services;
    }

    /// <summary>
    /// Realiza migração do banco de dados para a versão mais recente.
    /// </summary>
    public static async Task MigrateAsync(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }
}
