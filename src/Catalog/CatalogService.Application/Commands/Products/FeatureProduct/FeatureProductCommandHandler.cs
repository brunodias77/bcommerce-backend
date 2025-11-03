using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.Products.FeatureProduct;

public class FeatureProductCommandHandler : ICommandHandler<FeatureProductCommand, ApiResponse<FeatureProductResponse>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FeatureProductCommandHandler> _logger;

    public FeatureProductCommandHandler(
        IProductRepository productRepository, 
        IUnitOfWork unitOfWork,
        ILogger<FeatureProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<FeatureProductResponse>> HandleAsync(FeatureProductCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("⭐ [FeatureProductCommandHandler] Iniciando processamento para FeatureProductCommand - ProductId: {ProductId}", request.Id);
        
        // 1. Buscar o produto por ID
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Produto com ID {request.Id} não foi encontrado.");
        }

        // 2. Verificar se o produto não está deletado
        if (product.DeletedAt.HasValue)
        {
            throw new DomainException("Não é possível marcar um produto deletado como destaque.");
        }

        // 3. Verificar se o produto já está em destaque
        if (product.IsFeatured)
        {
            throw new DomainException("Produto já está marcado como destaque.");
        }

        // 4. Marcar o produto como destaque usando o método do domínio
        product.Feature();

        // 5. Atualizar no repositório
        _productRepository.Update(product);

        // 6. Persistir mudanças no banco (TransactionBehavior gerencia a transação automaticamente)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Criar resposta de sucesso
        var response = new FeatureProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            IsFeatured = product.IsFeatured,
            FeaturedAt = DateTime.UtcNow
        };

        _logger.LogInformation("✅ [FeatureProductCommandHandler] Processamento concluído com sucesso para FeatureProductCommand - ProductId: {ProductId}", request.Id);
        
        return ApiResponse<FeatureProductResponse>.Ok(response, "Produto marcado como destaque com sucesso.");
    }
}