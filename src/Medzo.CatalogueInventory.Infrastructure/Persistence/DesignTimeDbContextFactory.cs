using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Medzo.CatalogueInventory.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CatalogueInventoryDbContext>
{
    public CatalogueInventoryDbContext CreateDbContext(string[] args)
    {
        LoadEnvironmentFile();
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__CatalogueInventory");
        if (string.IsNullOrWhiteSpace(connection) ||
            connection.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
            connection.Contains("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Replace the ConnectionStrings__CatalogueInventory placeholders in the repository-root .env file before running EF migrations.");
        }

        var options = new DbContextOptionsBuilder<CatalogueInventoryDbContext>()
            .UseSqlServer(connection, builder =>
                builder.MigrationsAssembly(typeof(CatalogueInventoryDbContext).Assembly.FullName))
            .Options;
        return new CatalogueInventoryDbContext(options);
    }

    private static void LoadEnvironmentFile()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, ".env");
            if (File.Exists(path))
            {
                foreach (var rawLine in File.ReadLines(path))
                {
                    var line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith('#')) continue;
                    var separator = line.IndexOf('=');
                    if (separator <= 0) continue;
                    var key = line[..separator].Trim();
                    var value = line[(separator + 1)..].Trim();
                    if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
                    {
                        value = value[1..^1];
                    }
                    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
                    {
                        Environment.SetEnvironmentVariable(key, value);
                    }
                }
                return;
            }
            directory = directory.Parent;
        }
    }
}
