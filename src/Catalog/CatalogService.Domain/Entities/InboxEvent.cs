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
    
    public override ValidationHandler Validate()
    {
        throw new NotImplementedException();
    }
}