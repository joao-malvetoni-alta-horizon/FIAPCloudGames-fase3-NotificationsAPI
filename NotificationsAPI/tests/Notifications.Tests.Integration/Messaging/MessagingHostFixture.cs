using Amazon.DynamoDBv2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Notifications.Application.DependencyInjection;
using Notifications.Domain.Notifications;
using Notifications.Infrastructure.DependencyInjection;
using Notifications.Infrastructure.Persistence.DynamoDb;
using Notifications.Tests.Integration.Infrastructure.Persistence.DynamoDb;
using NSubstitute;
using Testcontainers.RabbitMq;

namespace Notifications.Tests.Integration.Messaging;

/// <summary>
/// Sobe um host genérico com a mesma composição de DI usada em produção
/// (<c>AddApplication</c> + <c>AddInfrastructure</c>), contra DynamoDB Local e RabbitMQ reais via
/// TestContainers, substituindo o <see cref="IEmailService"/> por um duplo de teste. Valida o fluxo
/// de mensageria ponta a ponta: evento publicado → consumidor → notificação persistida → email "enviado".
/// </summary>
/// <remarks>
/// Usa <see cref="IHost"/> em vez de <c>WebApplicationFactory&lt;Program&gt;</c> porque o projeto
/// Notifications.API foi removido. Os consumidores são registrados dentro de
/// <c>AddInfrastructure</c>, então o host genérico cumpre o papel de hospedar os
/// <c>BackgroundService</c> sem nenhuma dependência de HTTP.
/// </remarks>
public sealed class MessagingHostFixture : IAsyncLifetime
{
    private readonly DynamoDbTable _table = new();

    private IHost? _host;

    public RabbitMqContainer RabbitMqContainer { get; } = new RabbitMqBuilder()
        .WithUsername("guest")
        .WithPassword("guest")
        .WithCleanUp(true)
        .Build();

    public IEmailService EmailServiceSubstitute { get; } = Substitute.For<IEmailService>();

    /// <summary>Cliente apontado para o DynamoDB Local, para os testes verificarem o que foi gravado.</summary>
    public IAmazonDynamoDB DynamoDbClient => _table.Client;

    public DynamoDbOptions DynamoDbOptions => _table.Options;

    public IServiceProvider Services => _host?.Services
        ?? throw new InvalidOperationException("O host ainda não foi inicializado.");

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_table.StartAsync(), RabbitMqContainer.StartAsync());

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DynamoDb:TableName"] = _table.Options.TableName,
            ["DynamoDb:ServiceUrl"] = _table.ServiceUrl,
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

        // Sobe os RabbitMqConsumerHostedService. Cada teste ainda aguarda o sinal Started do
        // consumidor que lhe interessa antes de publicar. Não há migração a rodar: a tabela é
        // criada pelo fixture, como o template.yaml fará em produção.
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
        await _table.DisposeAsync();
    }
}
