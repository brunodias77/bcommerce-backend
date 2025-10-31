using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Validations;

namespace CatalogService.Domain.Entities;

public class InboxEvent : Entity
{
    
    public string EventType { get; private set; }
    public Guid AggregateId { get; private set; }
    public DateTime ProcessedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private InboxEvent() 
    {
        EventType = string.Empty;
    }

    public static InboxEvent Create(string eventType, Guid aggregateId)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType is required", nameof(eventType));

        if (aggregateId == Guid.Empty)
            throw new ArgumentException("AggregateId cannot be empty", nameof(aggregateId));

        return new InboxEvent
        {
            EventType = eventType,
            AggregateId = aggregateId,
            ProcessedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public override ValidationHandler Validate()
    {
        throw new NotImplementedException();
    }
}