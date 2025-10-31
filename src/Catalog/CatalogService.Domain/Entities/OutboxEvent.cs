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
    
    public override ValidationHandler Validate()
    {
        throw new NotImplementedException();
    }
}