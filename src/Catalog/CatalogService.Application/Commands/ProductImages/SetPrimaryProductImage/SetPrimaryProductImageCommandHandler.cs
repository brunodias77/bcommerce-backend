using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.ProductImages.SetPrimaryProductImage;

/// <summary>
/// Handler para o comando de definir imagem principal de produto
/// </summary>
public class SetPrimaryProductImageCommandHandler : ICommandHandler<SetPrimaryProductImageCommand, ApiResponse<bool>>
{
    private readonly IProductImageRepository _productImageRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetPrimaryProductImageCommandHandler> _logger;

    public SetPrimaryProductImageCommandHandler(
        IProductImageRepository productImageRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<SetPrimaryProductImageCommandHandler> logger)
    {
        _productImageRepository = productImageRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Executa o comando de definir imagem principal de produto
    /// </summary>
    /// <param name="request">Comando de definir imagem principal</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta da operação</returns>
    public async Task<ApiResponse<bool>> HandleAsync(SetPrimaryProductImageCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [SetPrimaryProductImageCommandHandler] Iniciando definição da imagem {ImageId} como principal do produto {ProductId}", 
            request.ImageId, request.ProductId);

        // 1. Validar se a imagem existe
        var targetImage = await _productImageRepository.GetByIdAsync(request.ImageId, cancellationToken);
        if (targetImage == null)
        {
            _logger.LogWarning("❌ [SetPrimaryProductImageCommandHandler] Imagem {ImageId} não encontrada", request.ImageId);
            throw new KeyNotFoundException($"Imagem com ID {request.ImageId} não foi encontrada");
        }

        // 2. Validar se a imagem pertence ao produto especificado
        if (targetImage.ProductId != request.ProductId)
        {
            _logger.LogWarning("❌ [SetPrimaryProductImageCommandHandler] Imagem {ImageId} não pertence ao produto {ProductId}", 
                request.ImageId, request.ProductId);
            throw new DomainException($"A imagem {request.ImageId} não pertence ao produto {request.ProductId}");
        }

        // 3. Validar se o produto existe e não foi deletado
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("❌ [SetPrimaryProductImageCommandHandler] Produto {ProductId} não encontrado", request.ProductId);
            throw new KeyNotFoundException($"Produto com ID {request.ProductId} não foi encontrado");
        }

        if (product.DeletedAt.HasValue)
        {
            _logger.LogWarning("❌ [SetPrimaryProductImageCommandHandler] Tentativa de definir imagem principal em produto deletado {ProductId}", 
                request.ProductId);
            throw new DomainException("Não é possível definir imagem principal de um produto que foi removido");
        }

        // 4. Verificar se a imagem já é a principal
        if (targetImage.IsPrimary)
        {
            _logger.LogInformation("ℹ️ [SetPrimaryProductImageCommandHandler] Imagem {ImageId} já é a principal do produto {ProductId}", 
                request.ImageId, request.ProductId);
            return ApiResponse<bool>.Ok(true, "Imagem já é a principal do produto");
        }

        // 5. Buscar todas as imagens do produto
        var productImages = await _productImageRepository.FindAsync(img => img.ProductId == request.ProductId, cancellationToken);

        // 6. Remover o status de principal de todas as outras imagens
        foreach (var image in productImages.Where(img => img.IsPrimary && img.Id != request.ImageId))
        {
            image.Update(
                image.Url,
                image.ThumbnailUrl,
                image.AltText,
                image.DisplayOrder,
                false // Remover status de principal
            );
            _productImageRepository.Update(image);
            
            _logger.LogInformation("ℹ️ [SetPrimaryProductImageCommandHandler] Removido status principal da imagem {ImageId}", image.Id);
        }

        // 7. Definir a imagem especificada como principal
        targetImage.Update(
            targetImage.Url,
            targetImage.ThumbnailUrl,
            targetImage.AltText,
            targetImage.DisplayOrder,
            true // Definir como principal
        );
        _productImageRepository.Update(targetImage);

        // 8. Persistir alterações
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✅ [SetPrimaryProductImageCommandHandler] Imagem {ImageId} definida como principal do produto {ProductId} com sucesso", 
            request.ImageId, request.ProductId);

        return ApiResponse<bool>.Ok(true, "Imagem definida como principal com sucesso");
    }
}