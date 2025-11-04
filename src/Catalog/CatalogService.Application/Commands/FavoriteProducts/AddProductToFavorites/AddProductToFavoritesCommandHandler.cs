using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.FavoriteProducts.AddProductToFavorites;

public class AddProductToFavoritesCommandHandler : 
    ICommandHandler<AddProductToFavoritesCommand, ApiResponse<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly IFavoriteProductRepository _favoriteProductRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddProductToFavoritesCommandHandler> _logger;

    public AddProductToFavoritesCommandHandler(
        IProductRepository productRepository,
        IFavoriteProductRepository favoriteProductRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddProductToFavoritesCommandHandler> logger)
    {
        _productRepository = productRepository;
        _favoriteProductRepository = favoriteProductRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> HandleAsync(
        AddProductToFavoritesCommand request, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("❤️ [AddProductToFavoritesCommandHandler] Iniciando processamento para UserId: {UserId}, ProductId: {ProductId}", 
            request.UserId, request.ProductId);
        
        // 1. Verificar se o produto existe e está ativo
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Produto com ID {request.ProductId} não foi encontrado.");
        }

        // 2. Verificar se o produto não está deletado
        if (product.DeletedAt.HasValue)
        {
            throw new DomainException("Não é possível favoritar um produto deletado.");
        }

        // 3. Verificar se o produto está ativo
        if (!product.IsActive)
        {
            throw new DomainException("Não é possível favoritar um produto inativo.");
        }

        // 4. Verificar se já existe favorito para este usuário/produto
        var existingFavorite = await _favoriteProductRepository.FindAsync(
            f => f.UserId == request.UserId && f.ProductId == request.ProductId, 
            cancellationToken);
        
        if (existingFavorite.Any())
        {
            throw new DomainException("Produto já está nos favoritos do usuário.");
        }

        // 5. Criar o favorito
        var favoriteProduct = FavoriteProduct.Create(request.UserId, request.ProductId);
        
        // 6. Adicionar ao repositório
        await _favoriteProductRepository.AddAsync(favoriteProduct, cancellationToken);

        // 7. Atualizar contador de favoritos no produto
        product.IncrementFavoriteCount();
        _productRepository.Update(product);

        // 8. Persistir mudanças (TransactionBehavior gerencia a transação)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✅ [AddProductToFavoritesCommandHandler] Favorito adicionado com sucesso para UserId: {UserId}, ProductId: {ProductId}", 
            request.UserId, request.ProductId);
        
        return ApiResponse<bool>.Ok(true, "Produto adicionado aos favoritos com sucesso.");
    }
}