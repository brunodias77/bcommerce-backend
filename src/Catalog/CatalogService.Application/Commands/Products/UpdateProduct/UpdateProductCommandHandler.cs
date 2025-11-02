using System.Linq;
using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Repository;
using CatalogService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.Products.UpdateProduct;

public class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, ApiResponse<UpdateProductResponse>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public UpdateProductCommandHandler(
        IProductRepository productRepository, 
        IUnitOfWork unitOfWork,
        ILogger<UpdateProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<UpdateProductResponse>> HandleAsync(UpdateProductCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [UpdateProductCommandHandler] Iniciando atualização do produto {ProductId}", request.Id);
        
        // 1. Buscar o produto existente
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Produto com ID {request.Id} não foi encontrado.");
        }

        // 2. Verificar se produto não foi excluído (soft delete)
        if (product.DeletedAt.HasValue)
        {
            throw new DomainException("Não é possível atualizar um produto excluído.");
        }

        // 3. Validar se slug não está sendo usado por outro produto
        if (product.Slug != request.Slug)
        {
            var existingProducts = await _productRepository.FindAsync(p => p.Slug == request.Slug && p.Id != request.Id, cancellationToken);
            if (existingProducts.Any())
            {
                throw new DomainException("Já existe outro produto com este slug.");
            }
        }

        // 4. Criar objetos Money para os preços
        var price = Money.Create(request.Price, request.Currency);
        Money? compareAtPrice = null;
        Money? costPrice = null;

        if (request.CompareAtPrice.HasValue)
            compareAtPrice = Money.Create(request.CompareAtPrice.Value, request.Currency);

        if (request.CostPrice.HasValue)
            costPrice = Money.Create(request.CostPrice.Value, request.Currency);

        // 5. Atualizar o produto usando o método Update
        product.Update(
            request.Name,
            request.Slug,
            price,
            request.Stock,
            request.Description,
            request.ShortDescription,
            compareAtPrice,
            costPrice,
            request.LowStockThreshold,
            request.CategoryId,
            request.MetaTitle,
            request.MetaDescription,
            request.WeightKg,
            request.Sku,
            request.Barcode,
            request.IsActive,
            request.IsFeatured
        );

        // 6. Atualizar no repositório
        _productRepository.Update(product);

        // 7. Persistir mudanças no banco
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 8. Criar resposta de sucesso
        var response = new UpdateProductResponse(
            product.Id,
            product.Name,
            product.Slug,
            product.Description,
            product.ShortDescription,
            product.Price.Amount,
            product.Price.Currency,
            product.CompareAtPrice?.Amount,
            product.CostPrice?.Amount,
            product.Stock,
            product.LowStockThreshold,
            product.CategoryId,
            product.MetaTitle,
            product.MetaDescription,
            product.WeightKg,
            product.Sku,
            product.Barcode,
            product.IsActive,
            product.IsFeatured,
            product.Version,
            product.CreatedAt,
            product.UpdatedAt
        );

        _logger.LogInformation("✅ [UpdateProductCommandHandler] Produto {ProductId} atualizado com sucesso", request.Id);
        
        return ApiResponse<UpdateProductResponse>.Ok(response, "Produto atualizado com sucesso.");
    }
}