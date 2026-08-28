using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Notifications.Application.DependencyInjection;
using Notifications.Domain.Notifications;
using Notifications.Infrastructure.DependencyInjection;
using NSubstitute;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Notifications.Tests.Integration.Messaging;

/// <summary>
/// Sobe um host genérico com a mesma composição de DI usada em produção
/// (<c>AddApplication</c> + <c>AddInfrastructure</c>), contra PostgreSQL e RabbitMQ reais via
/// TestContainers, substituindo o <see cref="IEmailService"/> por um duplo de teste. Valida o fluxo
/// de mensageria ponta a ponta: evento publicado → consumidor → notificação persistida → email "enviado".
/// </summary>
/// <remarks>
/// Usa <see cref="IHost"/> em vez de <c>WebApplicationFactory&lt;Program&gt;</c> porque o projeto
/// Notifications.API foi removido. Os consumidores são registrados dentro de
/// <c>AddInfrastructure</c>, não no antigo <c>Program.cs</c>, então nenhuma cobertura se perde —
/// o host web só existia para hospedar os <c>BackgroundService</c>, papel que o host genérico cumpre.
/// </remarks>
public sealed class MessagingHostFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithDatabase("notifications_messaging_test")
        .WithUsername("test")
        .WithPassword("test")
        .WithCleanUp(true)
        .Build();

    private IHost? _host;

    public RabbitMqContainer RabbitMqContainer { get; } = new RabbitMqBuilder()
        .WithUsername("guest")
        .WithPassword("guest")
        .WithCleanUp(true)
        .Build();

    public IEmailService EmailServiceSubstitute { get; } = Substitute.For<IEmailService>();

    /// <summary>
    /// Provedor de serviços do host, para os testes resolverem repositórios e consumidores.
    /// </summary>
    public IServiceProvider Services => _host?.Services
        ?? throw new InvalidOperationException("O host ainda não foi inicializado.");

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgresContainer.StartAsync(), RabbitMqContainer.StartAsync());

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = _postgresContainer.GetConnectionString(),
            ["RabbitMq:Host"] = RabbitMqContainer.Hostname,
            ["RabbitMq:Port"] = RabbitMqContainer.GetMappedPublicPort(5672).ToString(),
            ["RabbitMq:Username"] = "guest",
            ["RabbitMq:Password"] = "guest"
        });

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        builder.Services.RemoveAll<IEmailService>();
        builder.Services.AddSingleton(EmailServiceSubstitute);

        _host = builder.Build();

        // O antigo Program.cs migrava o banco no startup; aqui a responsabilidade é explícita.
        await _host.Services.MigrateAsync();

        // Sobe os RabbitMqConsumerHostedService. Cada teste ainda aguarda o sinal Started do
        // consumidor que lhe interessa antes de publicar.
        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        await RabbitMqContainer.StopAsync();
        await RabbitMqContainer.DisposeAsync();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }
}
