namespace NotificationsAPI.Infrastructure.Configuration;

using Serilog;
using Serilog.Core;
using Serilog.Events;

/// <summary>
/// Configuração para logging estruturado com Serilog.
/// </summary>
public static class SerilogConfig
{
    /// <summary>
    /// Configura Serilog com sinks de console e arquivo.
    /// </summary>
    public static Logger ConfigureLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information)
            .MinimumLevel.Override("MassTransit", LogEventLevel.Debug)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", "NotificationsAPI")
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
