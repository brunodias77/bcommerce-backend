using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.Products.DeleteProductImage;

/// <summary>
/// Handler para o comando de deletar imagem de produto
/// </summary>
public class DeleteProductImageCommandHandler : ICommandHandler<DeleteProductImageCommand, ApiResponse<bool>>
{
    private readonly IProductImageRepository _productImageRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteProductImageCommandHandler> _logger;

    public DeleteProductImageCommandHandler(
        IProductImageRepository productImageRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteProductImageCommandHandler> logger)
    {
        _productImageRepository = productImageRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Executa o comando de deletar imagem de produto
    /// </summary>
    /// <param name="request">Comando de deletar imagem</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta da operação</returns>
    public async Task<ApiResponse<bool>> HandleAsync(DeleteProductImageCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [DeleteProductImageCommandHandler] Iniciando exclusão da imagem {ImageId}", request.Id);

        // 1. Buscar imagem
        var productImage = await _productImageRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (productImage == null)
        {
            _logger.LogWarning("❌ [DeleteProductImageCommandHandler] Imagem {ImageId} não encontrada", request.Id);
            throw new KeyNotFoundException($"Imagem com ID {request.Id} não foi encontrada");
        }

        // 2. Validar se o produto ainda existe (não foi deletado)
        var product = await _productRepository.GetByIdAsync(productImage.ProductId, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("❌ [DeleteProductImageCommandHandler] Produto {ProductId} da imagem {ImageId} não encontrado", 
                productImage.ProductId, request.Id);
            throw new KeyNotFoundException($"Produto com ID {productImage.ProductId} não foi encontrado");
        }

        if (product.DeletedAt.HasValue)
        {
            _logger.LogWarning("❌ [DeleteProductImageCommandHandler] Tentativa de deletar imagem {ImageId} de produto já deletado {ProductId}", 
                request.Id, productImage.ProductId);
            throw new DomainException("Não é possível deletar imagem de um produto que já foi removido");
        }

        // 3. Verificar se é a única imagem do produto
        var productImages = await _productImageRepository.FindAsync(img => img.ProductId == productImage.ProductId, cancellationToken);
        var activeImages = productImages.Where(img => img.Id != request.Id).ToList();

        // Se esta é a imagem primária e há outras imagens, definir uma nova como primária
        if (productImage.IsPrimary && activeImages.Any())
        {
            var newPrimaryImage = activeImages.OrderBy(img => img.DisplayOrder).First();
            newPrimaryImage.Update(
                newPrimaryImage.Url,
                newPrimaryImage.ThumbnailUrl,
                newPrimaryImage.AltText,
                newPrimaryImage.DisplayOrder,
                true // Definir como primária
            );
            _productImageRepository.Update(newPrimaryImage);
            
            _logger.LogInformation("ℹ️ [DeleteProductImageCommandHandler] Imagem {NewPrimaryImageId} definida como nova imagem primária do produto {ProductId}", 
                newPrimaryImage.Id, productImage.ProductId);
        }

        // 4. Remover a imagem
        _productImageRepository.Remove(productImage);

        // 5. Persistir alterações
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✅ [DeleteProductImageCommandHandler] Imagem {ImageId} do produto {ProductId} excluída com sucesso", 
            request.Id, productImage.ProductId);

        return ApiResponse<bool>.Ok(true, "Imagem removida com sucesso");
    }
}