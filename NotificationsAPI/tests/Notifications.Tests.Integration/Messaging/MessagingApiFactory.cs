using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notifications.Domain.Notifications;
using NSubstitute;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Notifications.Tests.Integration.Messaging;

/// <summary>
/// Factory que sobe a API completa (Program.cs) contra PostgreSQL e RabbitMQ reais via TestContainers,
/// substituindo o <see cref="IEmailService"/> por um duplo de teste, para validar o fluxo de
/// mensageria ponta a ponta: evento publicado → consumidor → notificação persistida → email "enviado".
/// </summary>
public class MessagingApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithDatabase("notifications_messaging_test")
        .WithUsername("test")
        .WithPassword("test")
        .WithCleanUp(true)
        .Build();

    public RabbitMqContainer RabbitMqContainer { get; } = new RabbitMqBuilder()
        .WithUsername("guest")
        .WithPassword("guest")
        .WithCleanUp(true)
        .Build();

    public IEmailService EmailServiceSubstitute { get; } = Substitute.For<IEmailService>();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgresContainer.StartAsync(), RabbitMqContainer.StartAsync());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgresContainer.GetConnectionString(),
                ["RabbitMq:Host"] = RabbitMqContainer.Hostname,
                ["RabbitMq:Port"] = RabbitMqContainer.GetMappedPublicPort(5672).ToString(),
                ["RabbitMq:Username"] = "guest",
                ["RabbitMq:Password"] = "guest"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton(EmailServiceSubstitute);
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await RabbitMqContainer.StopAsync();
        await RabbitMqContainer.DisposeAsync();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }
}
