using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Validations;

namespace CatalogService.Domain.Entities;

public class ReceivedEvent : Entity
{
    public string EventType { get; private set; }
    public string SourceService { get; private set; }
    public string Payload { get; private set; } // JSON
    public bool Processed { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private ReceivedEvent() 
    {
        EventType = string.Empty;
        SourceService = string.Empty;
        Payload = string.Empty;
    }

    public static ReceivedEvent Create(string eventType, string sourceService, string payload)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType is required", nameof(eventType));

        if (string.IsNullOrWhiteSpace(sourceService))
            throw new ArgumentException("SourceService is required", nameof(sourceService));

        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload is required", nameof(payload));

        return new ReceivedEvent
        {
            EventType = eventType,
            SourceService = sourceService,
            Payload = payload,
            Processed = false,
            ProcessedAt = null,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public override ValidationHandler Validate()
    {
        throw new NotImplementedException();
    }
}