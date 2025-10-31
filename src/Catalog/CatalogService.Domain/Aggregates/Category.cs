using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Validations;

namespace CatalogService.Domain.Aggregates;

public class Category : AggregateRoot
{
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Description { get; private set; }
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; }
    public int DisplayOrder { get; private set; }
    public string Metadata { get; private set; } // JSON
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    private Category() 
    {
        Name = string.Empty;
        Slug = string.Empty;
        Metadata = string.Empty;
    }
    
    public override ValidationHandler Validate()
    {
        throw new NotImplementedException();
    }
}