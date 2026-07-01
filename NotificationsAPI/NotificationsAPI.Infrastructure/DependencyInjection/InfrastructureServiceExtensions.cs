namespace NotificationsAPI.Infrastructure.DependencyInjection;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationsAPI.Domain.Shared;
using NotificationsAPI.Infrastructure.Messaging;
using NotificationsAPI.Infrastructure.Persistence;

/// <summary>
/// Extension methods for registering infrastructure services.
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Adds infrastructure services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register RabbitMQ settings
        services.Configure<RabbitMqSettings>(
            configuration.GetSection(RabbitMqSettings.SectionName));

        // Register database context
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
        });

        // Register Unit of Work and repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Migrates the database to the latest version.
    /// </summary>
    public static async Task MigrateAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }
}
