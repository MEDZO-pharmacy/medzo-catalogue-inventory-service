using Confluent.Kafka;
using Medzo.CatalogueInventory.Application.Common;
using Medzo.CatalogueInventory.Infrastructure.Messaging;
using Medzo.CatalogueInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Medzo.CatalogueInventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "SqlServer";
        var connection = configuration.GetConnectionString("CatalogueInventory");

        if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(connection)) connection = "Data Source=.appdata/medzo_inventory.dev.db";
            services.AddDbContext<CatalogueInventoryDbContext>(options => options.UseSqlite(connection));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(connection) ||
                connection.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
                connection.Contains("YOUR_", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Replace the ConnectionStrings__CatalogueInventory placeholders, or set Database__Provider=Sqlite for local development.");
            }

            services.AddDbContext<CatalogueInventoryDbContext>(options => options.UseSqlServer(connection, sql =>
            {
                sql.MigrationsAssembly(typeof(CatalogueInventoryDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            }));
        }

        services.AddScoped<ICatalogueInventoryStore>(providerServices =>
            providerServices.GetRequiredService<CatalogueInventoryDbContext>());

        if (bool.TryParse(configuration["Kafka:Enabled"], out var kafkaEnabled) && kafkaEnabled)
        {
            var brokers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
            services.AddSingleton<IProducer<string, string>>(_ =>
                new ProducerBuilder<string, string>(new ProducerConfig
                {
                    BootstrapServers = brokers,
                    EnableIdempotence = true,
                    Acks = Acks.All
                }).Build());
            services.AddHostedService<OutboxPublisher>();
            services.AddHostedService<StockEventConsumer>();
        }

        return services;
    }
}
