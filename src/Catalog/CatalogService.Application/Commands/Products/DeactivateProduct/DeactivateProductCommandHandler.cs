using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.Products.DeactivateProduct;

public class DeactivateProductCommandHandler : ICommandHandler<DeactivateProductCommand, ApiResponse<DeactivateProductResponse>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeactivateProductCommandHandler> _logger;

    public DeactivateProductCommandHandler(
        IProductRepository productRepository, 
        IUnitOfWork unitOfWork,
        ILogger<DeactivateProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<DeactivateProductResponse>> HandleAsync(DeactivateProductCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [DeactivateProductCommandHandler] Iniciando processamento para DeactivateProductCommand - ProductId: {ProductId}", request.ProductId);
        
        // 1. Buscar o produto por ID
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Produto com ID {request.ProductId} não foi encontrado.");
        }

        // 2. Verificar se o produto não está deletado
        if (product.DeletedAt.HasValue)
        {
            throw new DomainException("Não é possível desativar um produto deletado.");
        }

        // 3. Verificar se o produto já está inativo
        if (!product.IsActive)
        {
            throw new DomainException("Produto já está inativo.");
        }

        // 4. Desativar o produto usando o método do domínio
        product.Deactivate();

        // 5. Atualizar no repositório
        _productRepository.Update(product);

        // 6. Persistir mudanças no banco (TransactionBehavior gerencia a transação automaticamente)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Criar resposta de sucesso
        var response = new DeactivateProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            IsActive = product.IsActive,
            UpdatedAt = product.UpdatedAt
        };

        _logger.LogInformation("✅ [DeactivateProductCommandHandler] Processamento concluído com sucesso para DeactivateProductCommand - ProductId: {ProductId}", request.ProductId);
        
        return ApiResponse<DeactivateProductResponse>.Ok(response, "Produto desativado com sucesso.");
    }
}