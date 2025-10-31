using System;
using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Validations;
using CatalogService.Domain.Entities;
using CatalogService.Domain.ValueObjects;

namespace CatalogService.Domain.Aggregates;

public class Product : AggregateRoot
{
    private readonly List<ProductImage> _images = new();
    
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Description { get; private set; }
    public string? ShortDescription { get; private set; }
    
    // Pricing
    public Money Price { get; private set; }
    public Money? CompareAtPrice { get; private set; }
    public Money? CostPrice { get; private set; }
    
    // Inventory
    public int Stock { get; private set; }
    public int StockReserved { get; private set; }
    public int LowStockThreshold { get; private set; }
    
    // Categorization
    public Guid? CategoryId { get; private set; }
    
    // SEO
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    
    // Attributes
    public decimal? WeightKg { get; private set; }
    public Dimensions? DimensionsCm { get; private set; }
    public string? Sku { get; private set; }
    public string? Barcode { get; private set; }
    
    // Status
    public bool IsActive { get; private set; }
    public bool IsFeatured { get; private set; }
    
    // Stats (denormalized)
    public int ViewCount { get; private set; }
    public int FavoriteCount { get; private set; }
    public int ReviewCount { get; private set; }
    public decimal ReviewAvgRating { get; private set; }
    
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();
    public int AvailableStock => Stock - StockReserved;
    
    private Product() 
    {
        Name = string.Empty;
        Slug = string.Empty;
        Price = Money.Zero;
    }
    
    public override ValidationHandler Validate()
    {
        throw new NotImplementedException();
    }
}
