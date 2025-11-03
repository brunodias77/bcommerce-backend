using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.Products.ActivateProduct;

public class ActivateProductCommandHandler : ICommandHandler<ActivateProductCommand, ApiResponse<ActivateProductResponse>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActivateProductCommandHandler> _logger;

    public ActivateProductCommandHandler(
        IProductRepository productRepository, 
        IUnitOfWork unitOfWork,
        ILogger<ActivateProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<ActivateProductResponse>> HandleAsync(ActivateProductCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [ActivateProductCommandHandler] Iniciando processamento para ActivateProductCommand - ProductId: {ProductId}", request.ProductId);
        
        // 1. Buscar o produto por ID
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Produto com ID {request.ProductId} não foi encontrado.");
        }

        // 2. Verificar se o produto não está deletado
        if (product.DeletedAt.HasValue)
        {
            throw new DomainException("Não é possível ativar um produto deletado.");
        }

        // 3. Verificar se o produto já está ativo
        if (product.IsActive)
        {
            throw new DomainException("Produto já está ativo.");
        }

        // 4. Ativar o produto usando o método do domínio
        product.Activate();

        // 5. Atualizar no repositório
        _productRepository.Update(product);

        // 6. Persistir mudanças no banco (TransactionBehavior gerencia a transação automaticamente)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Criar resposta de sucesso
        var response = new ActivateProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            IsActive = product.IsActive,
            UpdatedAt = product.UpdatedAt
        };

        _logger.LogInformation("✅ [ActivateProductCommandHandler] Processamento concluído com sucesso para ActivateProductCommand - ProductId: {ProductId}", request.ProductId);
        
        return ApiResponse<ActivateProductResponse>.Ok(response, "Produto ativado com sucesso.");
    }
}