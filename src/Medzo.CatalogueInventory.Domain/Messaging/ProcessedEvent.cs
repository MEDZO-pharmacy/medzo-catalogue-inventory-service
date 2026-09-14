using Medzo.CatalogueInventory.Domain.Common;
namespace Medzo.CatalogueInventory.Domain.Messaging;
public sealed class ProcessedEvent:Entity{private ProcessedEvent(){}public ProcessedEvent(Guid eventId,string topic){EventId=eventId;Topic=topic;ProcessedAtUtc=DateTime.UtcNow;}public Guid EventId{get;private set;}public string Topic{get;private set;}=null!;public DateTime ProcessedAtUtc{get;private set;}}
