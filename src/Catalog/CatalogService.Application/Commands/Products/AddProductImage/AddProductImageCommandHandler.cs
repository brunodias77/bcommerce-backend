using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.Products.AddProductImage;

public class AddProductImageCommandHandler : ICommandHandler<AddProductImageCommand, ApiResponse<AddProductImageResponse>>
{
    private readonly IProductImageRepository _productImageRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddProductImageCommandHandler> _logger;

    public AddProductImageCommandHandler(
        IProductImageRepository productImageRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddProductImageCommandHandler> logger)
    {
        _productImageRepository = productImageRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<AddProductImageResponse>> HandleAsync(AddProductImageCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [AddProductImageCommandHandler] Iniciando processamento para AddProductImageCommand");
        
        // 1. Validar se o produto existe e não foi deletado
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Produto com ID {request.ProductId} não foi encontrado.");
        }

        if (product.DeletedAt.HasValue)
        {
            throw new DomainException("Não é possível adicionar imagem a um produto deletado.");
        }

        // 2. Criar a imagem do produto usando o método factory
        var productImage = ProductImage.Create(
            request.ProductId,
            request.Url,
            request.ThumbnailUrl,
            request.AltText,
            request.DisplayOrder,
            request.IsPrimary
        );

        // 3. Salvar no repositório
        await _productImageRepository.AddAsync(productImage, cancellationToken);

        // 4. Persistir mudanças no banco
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Criar resposta de sucesso
        var response = new AddProductImageResponse
        {
            Id = productImage.Id,
            ProductId = productImage.ProductId,
            Url = productImage.Url,
            ThumbnailUrl = productImage.ThumbnailUrl,
            AltText = productImage.AltText,
            DisplayOrder = productImage.DisplayOrder,
            IsPrimary = productImage.IsPrimary,
            CreatedAt = productImage.CreatedAt
        };

        _logger.LogInformation("✅ [AddProductImageCommandHandler] Processamento concluído com sucesso para AddProductImageCommand");
        
        return ApiResponse<AddProductImageResponse>.Ok(response, "Imagem adicionada ao produto com sucesso.");
    }
}