using System;
using System.Text.RegularExpressions;
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

    public static Product Create(
        string name,
        string slug,
        Money price,
        int stock = 0,
        string? description = null,
        string? shortDescription = null,
        Guid? categoryId = null,
        string? sku = null,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug is required", nameof(slug));

        if (price == null)
            throw new ArgumentException("Price is required", nameof(price));

        if (stock < 0)
            throw new ArgumentException("Stock cannot be negative", nameof(stock));

        var product = new Product
        {
            Name = name,
            Slug = slug,
            Description = description,
            ShortDescription = shortDescription,
            Price = price,
            Stock = stock,
            StockReserved = 0,
            LowStockThreshold = 10,
            CategoryId = categoryId,
            Sku = sku,
            IsActive = isActive,
            IsFeatured = false,
            ViewCount = 0,
            FavoriteCount = 0,
            ReviewCount = 0,
            ReviewAvgRating = 0,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var validationResult = product.Validate();
        if (validationResult.HasErrors)
        {
            throw new ArgumentException($"Dados inválidos: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
        }

        return product;
    }
    
    public override ValidationHandler Validate()
    {
        var handler = new ValidationHandler();
        
        // Validar Name
        if (string.IsNullOrWhiteSpace(Name))
            handler.Add("Nome do produto é obrigatório");
        else if (Name.Length > 200)
            handler.Add("Nome do produto deve ter no máximo 200 caracteres");
        
        // Validar Slug
        if (string.IsNullOrWhiteSpace(Slug))
            handler.Add("Slug do produto é obrigatório");
        else if (Slug.Length > 200)
            handler.Add("Slug do produto deve ter no máximo 200 caracteres");
        else if (!IsValidSlug(Slug))
            handler.Add("Slug deve conter apenas letras minúsculas, números e hífens, sem espaços ou caracteres especiais");
        
        // Validar Description (opcional)
        if (!string.IsNullOrEmpty(Description) && Description.Length > 2000)
            handler.Add("Descrição do produto deve ter no máximo 2000 caracteres");
        
        // Validar ShortDescription (opcional)
        if (!string.IsNullOrEmpty(ShortDescription) && ShortDescription.Length > 500)
            handler.Add("Descrição curta do produto deve ter no máximo 500 caracteres");
        
        // Validar Price
        if (Price == null)
            handler.Add("Preço do produto é obrigatório");
        else if (Price.Amount <= 0)
            handler.Add("Preço do produto deve ser maior que zero");
        
        // Validar CompareAtPrice (opcional)
        if (CompareAtPrice != null && Price != null && CompareAtPrice.Amount <= Price.Amount)
            handler.Add("Preço de comparação deve ser maior que o preço do produto");
        
        // Validar CostPrice (opcional)
        if (CostPrice != null && CostPrice.Amount <= 0)
            handler.Add("Preço de custo deve ser maior que zero");
        
        // Validar Stock
        if (Stock < 0)
            handler.Add("Estoque deve ser maior ou igual a zero");
        
        // Validar StockReserved
        if (StockReserved < 0)
            handler.Add("Estoque reservado deve ser maior ou igual a zero");
        else if (StockReserved > Stock)
            handler.Add("Estoque reservado não pode ser maior que o estoque disponível");
        
        // Validar LowStockThreshold
        if (LowStockThreshold < 0)
            handler.Add("Limite de estoque baixo deve ser maior ou igual a zero");
        
        // Validar MetaTitle (opcional)
        if (!string.IsNullOrEmpty(MetaTitle) && MetaTitle.Length > 60)
            handler.Add("Meta título deve ter no máximo 60 caracteres");
        
        // Validar MetaDescription (opcional)
        if (!string.IsNullOrEmpty(MetaDescription) && MetaDescription.Length > 160)
            handler.Add("Meta descrição deve ter no máximo 160 caracteres");
        
        // Validar WeightKg (opcional)
        if (WeightKg.HasValue && WeightKg.Value <= 0)
            handler.Add("Peso deve ser maior que zero");
        
        // Validar Sku (opcional)
        if (!string.IsNullOrEmpty(Sku) && Sku.Length > 50)
            handler.Add("SKU deve ter no máximo 50 caracteres");
        
        // Validar Barcode (opcional)
        if (!string.IsNullOrEmpty(Barcode) && Barcode.Length > 50)
            handler.Add("Código de barras deve ter no máximo 50 caracteres");
        
        // Validar ReviewAvgRating
        if (ReviewAvgRating < 0 || ReviewAvgRating > 5)
            handler.Add("Avaliação média deve estar entre 0 e 5");
        
        return handler;
    }
    
    private static bool IsValidSlug(string slug)
    {
        // Slug deve conter apenas letras minúsculas, números e hífens
        // Não pode começar ou terminar com hífen
        var slugPattern = @"^[a-z0-9]+(?:-[a-z0-9]+)*$";
        return Regex.IsMatch(slug, slugPattern);
    }
}
