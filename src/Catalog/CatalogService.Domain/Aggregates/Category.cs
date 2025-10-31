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

    public static Category Create(
        string name,
        string slug,
        string? description = null,
        Guid? parentId = null,
        int displayOrder = 0,
        bool isActive = true,
        string metadata = "{}")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug is required", nameof(slug));

        if (displayOrder < 0)
            throw new ArgumentException("DisplayOrder cannot be negative", nameof(displayOrder));

        return new Category
        {
            Name = name,
            Slug = slug,
            Description = description,
            ParentId = parentId,
            IsActive = isActive,
            DisplayOrder = displayOrder,
            Metadata = metadata,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public override ValidationHandler Validate()
    {
        throw new NotImplementedException();
    }
}