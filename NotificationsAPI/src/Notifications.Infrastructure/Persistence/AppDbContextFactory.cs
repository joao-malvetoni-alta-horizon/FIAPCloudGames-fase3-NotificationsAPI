namespace Notifications.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Factory para criação do AppDbContext em tempo de design (migrations).
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// Cria uma instância do AppDbContext para operações de design-time como migrations.
    /// </summary>
    /// <param name="args">Argumentos passados pela ferramenta de design-time.</param>
    /// <returns>Uma instância configurada do AppDbContext.</returns>
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = "Host=localhost;Database=notifications_db;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
