namespace Notifications.Functions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notifications.Application.DependencyInjection;
using Notifications.Infrastructure.DependencyInjection;

/// <summary>
/// Monta o contêiner de injeção de dependência da função. Roda uma vez por "cold start" do
/// ambiente de execução do Lambda — não a cada invocação.
/// </summary>
public static class Startup
{
    public static IServiceProvider BuildServiceProvider()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();

        // O Lambda captura stdout automaticamente e o encaminha para o CloudWatch Logs; sem um
        // provider explícito, AddLogging() sozinho não emite nada.
        services.AddLogging(builder => builder.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
        }));
        services.AddApplication();
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
}
