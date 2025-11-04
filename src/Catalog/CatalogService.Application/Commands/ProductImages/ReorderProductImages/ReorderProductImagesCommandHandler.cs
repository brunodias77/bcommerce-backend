using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.ProductImages.ReorderProductImages;

/// <summary>
/// Handler para o comando de reordenar imagens de produto
/// </summary>
public class ReorderProductImagesCommandHandler : ICommandHandler<ReorderProductImagesCommand, ApiResponse<bool>>
{
    private readonly IProductImageRepository _productImageRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReorderProductImagesCommandHandler> _logger;

    public ReorderProductImagesCommandHandler(
        IProductImageRepository productImageRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReorderProductImagesCommandHandler> logger)
    {
        _productImageRepository = productImageRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Executa o comando de reordenar imagens de produto
    /// </summary>
    /// <param name="request">Comando de reordenar imagens</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta da operação</returns>
    public async Task<ApiResponse<bool>> HandleAsync(ReorderProductImagesCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [ReorderProductImagesCommandHandler] Iniciando reordenação de {Count} imagens do produto {ProductId}", 
            request.ImageOrders.Count, request.ProductId);

        // 1. Validar se o produto existe e não foi deletado
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("❌ [ReorderProductImagesCommandHandler] Produto {ProductId} não encontrado", request.ProductId);
            throw new KeyNotFoundException($"Produto com ID {request.ProductId} não foi encontrado");
        }

        if (product.DeletedAt.HasValue)
        {
            _logger.LogWarning("❌ [ReorderProductImagesCommandHandler] Tentativa de reordenar imagens de produto deletado {ProductId}", 
                request.ProductId);
            throw new DomainException("Não é possível reordenar imagens de um produto que foi removido");
        }

        // 2. Buscar todas as imagens especificadas
        var imageIds = request.ImageOrders.Select(io => io.ImageId).ToList();
        var images = await _productImageRepository.FindAsync(img => imageIds.Contains(img.Id), cancellationToken);

        // 3. Validar se todas as imagens existem
        if (images.Count != imageIds.Count)
        {
            var foundIds = images.Select(img => img.Id).ToList();
            var missingIds = imageIds.Except(foundIds).ToList();
            _logger.LogWarning("❌ [ReorderProductImagesCommandHandler] Imagens não encontradas: {MissingIds}", 
                string.Join(", ", missingIds));
            throw new KeyNotFoundException($"Imagens não encontradas: {string.Join(", ", missingIds)}");
        }

        // 4. Validar se todas as imagens pertencem ao produto especificado
        var invalidImages = images.Where(img => img.ProductId != request.ProductId).ToList();
        if (invalidImages.Any())
        {
            var invalidIds = invalidImages.Select(img => img.Id).ToList();
            _logger.LogWarning("❌ [ReorderProductImagesCommandHandler] Imagens {InvalidIds} não pertencem ao produto {ProductId}", 
                string.Join(", ", invalidIds), request.ProductId);
            throw new DomainException($"As imagens {string.Join(", ", invalidIds)} não pertencem ao produto {request.ProductId}");
        }

        // 5. Atualizar a ordem de exibição de cada imagem
        foreach (var imageOrder in request.ImageOrders)
        {
            var image = images.First(img => img.Id == imageOrder.ImageId);
            
            image.Update(
                image.Url,
                image.ThumbnailUrl,
                image.AltText,
                imageOrder.DisplayOrder, // Nova ordem
                image.IsPrimary
            );
            
            _productImageRepository.Update(image);
            
            _logger.LogInformation("ℹ️ [ReorderProductImagesCommandHandler] Imagem {ImageId} reordenada para posição {DisplayOrder}", 
                imageOrder.ImageId, imageOrder.DisplayOrder);
        }

        // 6. Persistir alterações
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✅ [ReorderProductImagesCommandHandler] Reordenação de {Count} imagens do produto {ProductId} concluída com sucesso", 
            request.ImageOrders.Count, request.ProductId);

        return ApiResponse<bool>.Ok(true, "Imagens reordenadas com sucesso");
    }
}