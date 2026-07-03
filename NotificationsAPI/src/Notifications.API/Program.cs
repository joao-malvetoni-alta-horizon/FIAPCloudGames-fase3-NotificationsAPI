using Serilog;
using NotificationsAPI.Infrastructure.DependencyInjection;
using NotificationsAPI.Application.DependencyInjection;
using Notifications.API.Middleware;

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
