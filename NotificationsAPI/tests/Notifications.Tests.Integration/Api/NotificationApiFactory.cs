using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Notifications.Tests.Integration.Api;

/// <summary>
/// Factory que sobe a API completa (Program.cs) contra um PostgreSQL real via TestContainers,
/// permitindo testes de integração ponta a ponta contra os endpoints HTTP.
/// </summary>
public class NotificationApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithDatabase("notifications_api_test")
        .WithUsername("test")
        .WithPassword("test")
        .WithCleanUp(true)
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _container.GetConnectionString()
            });
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }
}
