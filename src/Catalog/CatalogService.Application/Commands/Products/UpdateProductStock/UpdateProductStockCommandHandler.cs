using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.Products.UpdateProductStock;

public class UpdateProductStockCommandHandler : ICommandHandler<UpdateProductStockCommand, ApiResponse<UpdateProductStockResponse>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProductStockCommandHandler> _logger;

    public UpdateProductStockCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateProductStockCommandHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<UpdateProductStockResponse>> HandleAsync(UpdateProductStockCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [UpdateProductStockCommandHandler] Iniciando atualização de estoque do produto {ProductId}", request.Id);
        
        // 1. Buscar o produto existente
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Produto com ID {request.Id} não foi encontrado.");
        }

        // 2. Verificar se produto não foi excluído (soft delete)
        if (product.DeletedAt.HasValue)
        {
            throw new DomainException("Não é possível atualizar estoque de um produto excluído.");
        }

        // 3. Atualizar o estoque usando o método do agregado
        product.UpdateStock(request.Stock, request.Operation);

        // 4. Atualizar no repositório
        _productRepository.Update(product);

        // 5. Persistir mudanças no banco
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Criar resposta de sucesso
        var response = new UpdateProductStockResponse(
            product.Id,
            product.Name,
            product.Stock,
            product.AvailableStock,
            product.UpdatedAt
        );

        _logger.LogInformation("✅ [UpdateProductStockCommandHandler] Estoque do produto {ProductId} atualizado com sucesso. Novo estoque: {Stock}", request.Id, product.Stock);
        
        return ApiResponse<UpdateProductStockResponse>.Ok(response, "Estoque do produto atualizado com sucesso.");
    }
}