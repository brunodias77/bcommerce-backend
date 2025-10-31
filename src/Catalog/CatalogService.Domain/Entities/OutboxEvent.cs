using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Validations;
using CatalogService.Domain.Enums;

namespace CatalogService.Domain.Entities;

public class OutboxEvent : Entity
{
    public Guid AggregateId { get; private set; }
    public string AggregateType { get; private set; }
    public string EventType { get; private set; }
    public int EventVersion { get; private set; }
    public string Payload { get; private set; } // JSON
    public string Metadata { get; private set; } // JSON
    public OutboxStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    private OutboxEvent() 
    {
        AggregateType = string.Empty;
        EventType = string.Empty;
        Payload = string.Empty;
        Metadata = string.Empty;
    }

    public static OutboxEvent Create(
        Guid aggregateId,
        string aggregateType,
        string eventType,
        int eventVersion,
        string payload,
        string metadata = "{}",
        int maxRetries = 3)
    {
        if (aggregateId == Guid.Empty)
            throw new ArgumentException("AggregateId cannot be empty", nameof(aggregateId));

        if (string.IsNullOrWhiteSpace(aggregateType))
            throw new ArgumentException("AggregateType is required", nameof(aggregateType));

        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType is required", nameof(eventType));

        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload is required", nameof(payload));

        if (eventVersion < 1)
            throw new ArgumentException("EventVersion must be positive", nameof(eventVersion));

        if (maxRetries < 0)
            throw new ArgumentException("MaxRetries cannot be negative", nameof(maxRetries));

        return new OutboxEvent
        {
            AggregateId = aggregateId,
            AggregateType = aggregateType,
            EventType = eventType,
            EventVersion = eventVersion,
            Payload = payload,
            Metadata = metadata,
            Status = OutboxStatus.Pending,
            RetryCount = 0,
            MaxRetries = maxRetries,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public override ValidationHandler Validate()
    {
        throw new NotImplementedException();
    }
}