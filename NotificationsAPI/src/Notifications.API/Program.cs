using Serilog;
using Notifications.Infrastructure.DependencyInjection;
using Notifications.Application.DependencyInjection;
using Notifications.API.Middleware;
using Notifications.API.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .MinimumLevel.Information()
        .WriteTo.Console()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "NotificationsAPI");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.Services.MigrateAsync();

app.UseExceptionMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthCheck();
app.MapNotificationEndpoints();

app.Run();

/// <summary>
/// Marcador público para permitir o uso de <c>WebApplicationFactory&lt;Program&gt;</c> em testes de integração.
/// </summary>
public partial class Program;
