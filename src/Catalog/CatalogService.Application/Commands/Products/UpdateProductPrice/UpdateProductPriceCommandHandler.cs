using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Repository;
using CatalogService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.Products.UpdateProductPrice;

public class UpdateProductPriceCommandHandler : ICommandHandler<UpdateProductPriceCommand, ApiResponse<UpdateProductPriceResponse>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProductPriceCommandHandler> _logger;

    public UpdateProductPriceCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateProductPriceCommandHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<UpdateProductPriceResponse>> HandleAsync(UpdateProductPriceCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [UpdateProductPriceCommandHandler] Iniciando atualização de preço do produto {ProductId}", request.Id);
        
        // 1. Buscar o produto existente
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Produto com ID {request.Id} não foi encontrado.");
        }

        // 2. Verificar se produto não foi excluído (soft delete)
        if (product.DeletedAt.HasValue)
        {
            throw new DomainException("Não é possível atualizar preço de um produto excluído.");
        }

        // 3. Converter decimais para Money
        var price = Money.Create(request.Price);
        var compareAtPrice = request.CompareAtPrice.HasValue ? Money.Create(request.CompareAtPrice.Value) : null;

        // 4. Atualizar o preço usando o método do agregado
        product.UpdatePrice(price, compareAtPrice);

        // 5. Atualizar no repositório
        _productRepository.Update(product);

        // 6. Persistir mudanças no banco
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Criar resposta de sucesso
        var response = new UpdateProductPriceResponse(
            product.Id,
            product.Name,
            product.Price.Amount,
            product.CompareAtPrice?.Amount,
            product.UpdatedAt
        );

        _logger.LogInformation("✅ [UpdateProductPriceCommandHandler] Preço do produto {ProductId} atualizado com sucesso. Novo preço: {Price}", request.Id, product.Price.Amount);
        
        return ApiResponse<UpdateProductPriceResponse>.Ok(response, "Preço do produto atualizado com sucesso.");
    }
}