using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.Products.UpdateProductImage;

public class UpdateProductImageCommandHandler : ICommandHandler<UpdateProductImageCommand, ApiResponse<UpdateProductImageResponse>>
{
    private readonly IProductImageRepository _productImageRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProductImageCommandHandler> _logger;

    public UpdateProductImageCommandHandler(
        IProductImageRepository productImageRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateProductImageCommandHandler> logger)
    {
        _productImageRepository = productImageRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<UpdateProductImageResponse>> HandleAsync(UpdateProductImageCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [UpdateProductImageCommandHandler] Iniciando processamento para UpdateProductImageCommand");
        
        // 1. Validar se a imagem existe
        var productImage = await _productImageRepository.GetByIdAsync(request.Id, cancellationToken);
        if (productImage == null)
        {
            throw new KeyNotFoundException($"Imagem com ID {request.Id} não foi encontrada.");
        }

        // 2. Validar se o produto existe e não foi deletado
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Produto com ID {request.ProductId} não foi encontrado.");
        }

        if (product.DeletedAt.HasValue)
        {
            throw new DomainException("Não é possível atualizar imagem de um produto deletado.");
        }

        // 3. Atualizar as propriedades da imagem usando o método Update da entidade
        productImage.Update(
            request.Url,
            request.ThumbnailUrl,
            request.AltText,
            request.DisplayOrder,
            request.IsPrimary
        );

        // 4. Atualizar no repositório
        _productImageRepository.Update(productImage);

        // 5. Persistir mudanças no banco
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Criar resposta de sucesso
        var response = new UpdateProductImageResponse
        {
            Id = productImage.Id,
            ProductId = productImage.ProductId,
            Url = productImage.Url,
            ThumbnailUrl = productImage.ThumbnailUrl,
            AltText = productImage.AltText,
            DisplayOrder = productImage.DisplayOrder,
            IsPrimary = productImage.IsPrimary,
            UpdatedAt = productImage.UpdatedAt
        };

        _logger.LogInformation("✅ [UpdateProductImageCommandHandler] Processamento concluído com sucesso para UpdateProductImageCommand");
        
        return ApiResponse<UpdateProductImageResponse>.Ok(response, "Imagem do produto atualizada com sucesso.");
    }
}