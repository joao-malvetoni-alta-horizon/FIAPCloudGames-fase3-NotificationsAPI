namespace NotificationsAPI.Infrastructure.Configuration;

using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;

/// <summary>
/// Configuration for Serilog structured logging.
/// </summary>
public static class SerilogConfig
{
    /// <summary>
    /// Configures Serilog with console and file sinks.
    /// </summary>
    public static Logger ConfigureLogger(IConfiguration configuration)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information)
            .MinimumLevel.Override("MassTransit", LogEventLevel.Debug)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", "NotificationsAPI")
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
