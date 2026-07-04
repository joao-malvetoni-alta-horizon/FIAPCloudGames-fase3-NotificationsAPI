namespace Notifications.Infrastructure.DependencyInjection;

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
        // Registrar configurações do RabbitMQ
        services.Configure<RabbitMqSettings>(
            configuration.GetSection(RabbitMqSettings.SectionName));

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

        // Registrar serviço de email e consumidor RabbitMQ
        services.AddSingleton<IEmailService, EmailService>();
        services.AddSingleton<IEventDispatcher, EventDispatcher>();
        services.AddSingleton<UserRegisteredEventMessageProcessor>();
        services.AddSingleton<RabbitMqConsumerHostedService>();
        services.AddHostedService(sp => sp.GetRequiredService<RabbitMqConsumerHostedService>());

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
