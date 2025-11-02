using System.Linq;
using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Repository;
using CatalogService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.Products.CreateProduct;

public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, ApiResponse<CreateProductResponse>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        IProductRepository productRepository, 
        IUnitOfWork unitOfWork,
        ILogger<CreateProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<CreateProductResponse>> HandleAsync(CreateProductCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [CreateProductCommandHandler] Iniciando processamento para CreateProductCommand");
        
        // Validação será feita automaticamente pelo ValidationBehavior
        
        // 1. Validar se já existe produto com o mesmo slug
        var existingProducts = await _productRepository.FindAsync(p => p.Slug == request.Slug, cancellationToken);
        if (existingProducts.Any())
        {
            throw new DomainException("Já existe um produto com este slug.");
        }

        // 2. Criar o objeto Money para o preço
        var price = Money.Create(request.Price, request.Currency);
        Money? compareAtPrice = null;
        Money? costPrice = null;

        if (request.CompareAtPrice.HasValue)
            compareAtPrice = Money.Create(request.CompareAtPrice.Value, request.Currency);

        if (request.CostPrice.HasValue)
            costPrice = Money.Create(request.CostPrice.Value, request.Currency);

        // 3. Criar o produto usando o método factory
        var product = Product.Create(
            request.Name,
            request.Slug,
            price,
            request.Stock,
            request.Description,
            request.ShortDescription,
            request.CategoryId,
            request.Sku,
            request.IsActive
        );

        // 4. Definir propriedades adicionais que não estão no método Create
        if (compareAtPrice != null)
            product.GetType().GetProperty("CompareAtPrice")?.SetValue(product, compareAtPrice);
        
        if (costPrice != null)
            product.GetType().GetProperty("CostPrice")?.SetValue(product, costPrice);
        
        if (request.LowStockThreshold != 10) // 10 é o valor padrão
            product.GetType().GetProperty("LowStockThreshold")?.SetValue(product, request.LowStockThreshold);
        
        if (!string.IsNullOrEmpty(request.MetaTitle))
            product.GetType().GetProperty("MetaTitle")?.SetValue(product, request.MetaTitle);
        
        if (!string.IsNullOrEmpty(request.MetaDescription))
            product.GetType().GetProperty("MetaDescription")?.SetValue(product, request.MetaDescription);
        
        if (request.WeightKg.HasValue)
            product.GetType().GetProperty("WeightKg")?.SetValue(product, request.WeightKg);
        
        if (!string.IsNullOrEmpty(request.Barcode))
            product.GetType().GetProperty("Barcode")?.SetValue(product, request.Barcode);
        
        if (request.IsFeatured)
            product.GetType().GetProperty("IsFeatured")?.SetValue(product, request.IsFeatured);

        // 5. Salvar no repositório
        await _productRepository.AddAsync(product, cancellationToken);

        // 6. Persistir mudanças no banco (TransactionBehavior gerencia a transação automaticamente)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Criar resposta de sucesso
        var response = new CreateProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            ShortDescription = product.ShortDescription,
            Price = product.Price.Amount,
            Currency = product.Price.Currency,
            CompareAtPrice = product.CompareAtPrice?.Amount,
            CostPrice = product.CostPrice?.Amount,
            Stock = product.Stock,
            LowStockThreshold = product.LowStockThreshold,
            CategoryId = product.CategoryId,
            MetaTitle = product.MetaTitle,
            MetaDescription = product.MetaDescription,
            WeightKg = product.WeightKg,
            Sku = product.Sku,
            Barcode = product.Barcode,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };

        _logger.LogInformation("✅ [CreateProductCommandHandler] Processamento concluído com sucesso para CreateProductCommand");
        
        return ApiResponse<CreateProductResponse>.Ok(response, "Produto criado com sucesso.");
    }
}