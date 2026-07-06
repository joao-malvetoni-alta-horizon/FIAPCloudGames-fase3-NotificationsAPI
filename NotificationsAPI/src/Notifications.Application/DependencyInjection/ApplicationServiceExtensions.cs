namespace Notifications.Application.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;
using UseCases.Handlers;
using FiapCloudGames.Contracts.Payments;
using FiapCloudGames.Contracts.Users;

/// <summary>
/// Métodos de extensão para registrar serviços da camada Application.
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Adiciona serviços da Application ao contêiner de injeção de dependência.
    /// Registra todos os event handlers.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registrar event handlers
        services.AddScoped<IEventHandler<UserRegisteredEvent>, UserRegisteredEventHandler>();
        services.AddScoped<IEventHandler<PaymentProcessedEvent>, PaymentProcessedEventHandler>();

        return services;
    }
}
