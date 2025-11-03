using System;
using System.Text.RegularExpressions;
using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Validations;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Enums;
using CatalogService.Domain.Events.Products;
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
        return new Product
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


    }
    
    /// <summary>
    /// Atualiza os dados do produto
    /// </summary>
    /// <param name="name">Nome do produto</param>
    /// <param name="slug">Slug do produto</param>
    /// <param name="price">Preço do produto</param>
    /// <param name="stock">Estoque do produto</param>
    /// <param name="description">Descrição do produto</param>
    /// <param name="shortDescription">Descrição curta do produto</param>
    /// <param name="compareAtPrice">Preço de comparação</param>
    /// <param name="costPrice">Preço de custo</param>
    /// <param name="lowStockThreshold">Limite de estoque baixo</param>
    /// <param name="categoryId">ID da categoria</param>
    /// <param name="metaTitle">Meta título para SEO</param>
    /// <param name="metaDescription">Meta descrição para SEO</param>
    /// <param name="weightKg">Peso em quilogramas</param>
    /// <param name="sku">SKU do produto</param>
    /// <param name="barcode">Código de barras</param>
    /// <param name="isActive">Se o produto está ativo</param>
    /// <param name="isFeatured">Se o produto é destaque</param>
    /// <returns>Instância atualizada do produto</returns>
    public Product Update(
        string name,
        string slug,
        Money price,
        int stock,
        string? description = null,
        string? shortDescription = null,
        Money? compareAtPrice = null,
        Money? costPrice = null,
        int lowStockThreshold = 10,
        Guid? categoryId = null,
        string? metaTitle = null,
        string? metaDescription = null,
        decimal? weightKg = null,
        string? sku = null,
        string? barcode = null,
        bool isActive = true,
        bool isFeatured = false)
    {
        Name = name;
        Slug = slug;
        Description = description;
        ShortDescription = shortDescription;
        Price = price;
        CompareAtPrice = compareAtPrice;
        CostPrice = costPrice;
        Stock = stock;
        LowStockThreshold = lowStockThreshold;
        CategoryId = categoryId;
        MetaTitle = metaTitle;
        MetaDescription = metaDescription;
        WeightKg = weightKg;
        Sku = sku;
        Barcode = barcode;
        IsActive = isActive;
        IsFeatured = isFeatured;
        UpdatedAt = DateTime.UtcNow;
        Version++;
        
        return this;
    }
    
    /// <summary>
    /// Realiza o soft delete do produto
    /// </summary>
    /// <returns>Produto com soft delete aplicado</returns>
    public Product SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Version++;
        
        return this;
    }
    
    /// <summary>
    /// Ativa o produto
    /// </summary>
    /// <returns>Produto ativado</returns>
    public Product Activate()
    {
        if (IsActive)
            throw new DomainException("Produto já está ativo");
            
        if (DeletedAt.HasValue)
            throw new DomainException("Não é possível ativar um produto deletado");
        
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        Version++;
        
        // Adicionar evento de domínio
        AddDomainEvent(new ProductActivatedEvent(Id, Name, DateTime.UtcNow));
        
        return this;
    }
    
    /// <summary>
    /// Desativa o produto
    /// </summary>
    /// <returns>Produto desativado</returns>
    public Product Deactivate()
    {
        if (!IsActive)
            throw new DomainException("Produto já está inativo");
            
        if (DeletedAt.HasValue)
            throw new DomainException("Não é possível desativar um produto deletado");
        
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        Version++;
        
        // Adicionar evento de domínio
        AddDomainEvent(new ProductDeactivatedEvent(Id, Name, DateTime.UtcNow));
        
        return this;
    }
    
    /// <summary>
    /// Marca o produto como destaque
    /// </summary>
    /// <returns>Produto marcado como destaque</returns>
    public Product Feature()
    {
        if (IsFeatured)
            throw new DomainException("Produto já está marcado como destaque");
            
        if (DeletedAt.HasValue)
            throw new DomainException("Não é possível marcar um produto deletado como destaque");
        
        IsFeatured = true;
        UpdatedAt = DateTime.UtcNow;
        Version++;
        
        // Adicionar evento de domínio
        AddDomainEvent(new ProductFeaturedEvent(Id, Name, DateTime.UtcNow));
        
        return this;
    }
    
    /// <summary>
    /// Remove o produto dos destaques
    /// </summary>
    /// <returns>Produto removido dos destaques</returns>
    public Product Unfeature()
    {
        if (!IsFeatured)
            throw new DomainException("Produto não está marcado como destaque");
            
        if (DeletedAt.HasValue)
            throw new DomainException("Não é possível alterar status de destaque de um produto deletado");
        
        IsFeatured = false;
        UpdatedAt = DateTime.UtcNow;
        Version++;
        
        // Adicionar evento de domínio
        AddDomainEvent(new ProductUnfeaturedEvent(Id, Name, DateTime.UtcNow));
        
        return this;
    }
    
    /// <summary>
    /// Atualiza o estoque do produto
    /// </summary>
    /// <param name="quantity">Quantidade para a operação</param>
    /// <param name="operation">Tipo de operação (ADD, SUBTRACT, SET)</param>
    /// <returns>Produto com estoque atualizado</returns>
    public Product UpdateStock(int quantity, StockOperation operation)
    {
        if (DeletedAt.HasValue)
            throw new DomainException("Não é possível atualizar estoque de um produto deletado");
        
        if (quantity < 0)
            throw new DomainException("Quantidade deve ser maior ou igual a zero");
        
        var previousStock = Stock;
        var newStock = operation switch
        {
            StockOperation.ADD => Stock + quantity,
            StockOperation.SUBTRACT => Stock - quantity,
            StockOperation.SET => quantity,
            _ => throw new DomainException("Operação de estoque inválida")
        };
        
        if (newStock < 0)
            throw new DomainException("Estoque não pode ser negativo");
        
        Stock = newStock;
        UpdatedAt = DateTime.UtcNow;
        Version++;
        
        // Adicionar evento de domínio
        AddDomainEvent(new ProductStockUpdatedEvent(Id, Name, previousStock, newStock, operation, DateTime.UtcNow));
        
        return this;
    }
    
    /// <summary>
    /// Atualiza o preço do produto
    /// </summary>
    /// <param name="price">Novo preço do produto</param>
    /// <param name="compareAtPrice">Preço de comparação (opcional)</param>
    /// <returns>Produto com preço atualizado</returns>
    public Product UpdatePrice(Money price, Money? compareAtPrice = null)
    {
        if (DeletedAt.HasValue)
            throw new DomainException("Não é possível atualizar preço de um produto deletado");
        
        if (price == null)
            throw new DomainException("Preço é obrigatório");
        
        if (price.Amount <= 0)
            throw new DomainException("Preço deve ser maior que zero");
        
        if (compareAtPrice != null && compareAtPrice.Amount <= price.Amount)
            throw new DomainException("Preço de comparação deve ser maior que o preço do produto");
        
        var previousPrice = Price;
        var previousCompareAtPrice = CompareAtPrice;
        
        Price = price;
        CompareAtPrice = compareAtPrice;
        UpdatedAt = DateTime.UtcNow;
        Version++;
        
        // Adicionar evento de domínio
        AddDomainEvent(new ProductPriceUpdatedEvent(Id, Name, previousPrice, price, previousCompareAtPrice, compareAtPrice, DateTime.UtcNow));
        
        return this;
    }
    
    public override ValidationHandler Validate(ValidationHandler handler)
    {
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
