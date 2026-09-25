using Confluent.Kafka;
using Medzo.CatalogueInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Medzo.CatalogueInventory.Infrastructure.Messaging;

public sealed class OutboxPublisher(
    IServiceScopeFactory scopes,
    IProducer<string, string> producer,
    ILogger<OutboxPublisher> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CatalogueInventoryDbContext>();
                var rows = await db.OutboxMessages
                    .Where(message => message.ProcessedAtUtc == null && message.Attempts < 10)
                    .OrderBy(message => message.OccurredAtUtc)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var row in rows)
                {
                    try
                    {
                        await producer.ProduceAsync(row.Topic, new Message<string, string>
                        {
                            Key = row.Key,
                            Value = row.Payload
                        }, stoppingToken);
                        row.MarkProcessed();
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception error)
                    {
                        row.MarkFailed(error.Message);
                        log.LogError(error, "Publishing outbox message {MessageId} failed", row.Id);
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception error)
            {
                // A temporary SQL/network outage must not terminate the API process.
                // The next timer cycle retries the unpublished outbox rows.
                log.LogError(error, "The outbox polling cycle failed and will be retried");
            }
        }
    }
}
