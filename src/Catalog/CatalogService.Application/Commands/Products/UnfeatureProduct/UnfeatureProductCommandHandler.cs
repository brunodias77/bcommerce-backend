using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.Products.UnfeatureProduct;

public class UnfeatureProductCommandHandler : ICommandHandler<UnfeatureProductCommand, ApiResponse<UnfeatureProductResponse>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnfeatureProductCommandHandler> _logger;

    public UnfeatureProductCommandHandler(
        IProductRepository productRepository, 
        IUnitOfWork unitOfWork,
        ILogger<UnfeatureProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<UnfeatureProductResponse>> HandleAsync(UnfeatureProductCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🚫 [UnfeatureProductCommandHandler] Iniciando processamento para UnfeatureProductCommand - ProductId: {ProductId}", request.Id);
        
        // 1. Buscar o produto por ID
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Produto com ID {request.Id} não foi encontrado.");
        }

        // 2. Verificar se o produto não está deletado
        if (product.DeletedAt.HasValue)
        {
            throw new DomainException("Não é possível alterar status de destaque de um produto deletado.");
        }

        // 3. Verificar se o produto está em destaque
        if (!product.IsFeatured)
        {
            throw new DomainException("Produto não está marcado como destaque.");
        }

        // 4. Remover o produto dos destaques usando o método do domínio
        product.Unfeature();

        // 5. Atualizar no repositório
        _productRepository.Update(product);

        // 6. Persistir mudanças no banco (TransactionBehavior gerencia a transação automaticamente)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Criar resposta de sucesso
        var response = new UnfeatureProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            IsFeatured = product.IsFeatured,
            UnfeaturedAt = DateTime.UtcNow
        };

        _logger.LogInformation("✅ [UnfeatureProductCommandHandler] Processamento concluído com sucesso para UnfeatureProductCommand - ProductId: {ProductId}", request.Id);
        
        return ApiResponse<UnfeatureProductResponse>.Ok(response, "Produto removido dos destaques com sucesso.");
    }
}