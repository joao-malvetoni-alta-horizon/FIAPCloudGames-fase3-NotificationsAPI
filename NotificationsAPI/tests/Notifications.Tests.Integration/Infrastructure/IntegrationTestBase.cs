namespace NotificationsAPI.Tests.Integration.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Notifications.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

/// <summary>
/// Classe base para testes de integração com TestContainers.
/// Gerencia o ciclo de vida do container PostgreSQL e fornece um DbContext para testes.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    protected AppDbContext DbContext = null!;
    protected IServiceProvider ServiceProvider = null!;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithDatabase("notifications_test")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        DbContext = new AppDbContext(optionsBuilder.Options);

        await DbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }

        await DbContext.DisposeAsync();
    }
}
