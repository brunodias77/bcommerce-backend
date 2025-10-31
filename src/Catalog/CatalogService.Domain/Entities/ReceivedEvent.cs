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
    
    public override ValidationHandler Validate()
    {
        throw new NotImplementedException();
    }
}