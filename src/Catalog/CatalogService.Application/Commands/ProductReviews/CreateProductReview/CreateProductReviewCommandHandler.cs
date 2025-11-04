using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Repository;
using CatalogService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.ProductReviews.CreateProductReview;

public class CreateProductReviewCommandHandler : ICommandHandler<CreateProductReviewCommand, ApiResponse<ProductReviewResponse>>
{
    private readonly IProductReviewRepository _productReviewRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateProductReviewCommandHandler> _logger;

    public CreateProductReviewCommandHandler(
        IProductReviewRepository productReviewRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateProductReviewCommandHandler> logger)
    {
        _productReviewRepository = productReviewRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<ProductReviewResponse>> HandleAsync(CreateProductReviewCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("⭐ [CreateProductReviewCommandHandler] Iniciando processamento para CreateProductReviewCommand");
        
        // 1. Verificar se o produto existe e não está deletado
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null || product.DeletedAt.HasValue)
        {
            throw new DomainException("Produto não encontrado ou foi removido.");
        }

        // 2. Verificar se o usuário já avaliou este produto (evitar duplicatas)
        var existingReviews = await _productReviewRepository.FindAsync(
            r => r.ProductId == request.ProductId && r.UserId == request.UserId, 
            cancellationToken);
        
        if (existingReviews.Any())
        {
            throw new DomainException("Usuário já avaliou este produto.");
        }

        // 3. Criar o Rating usando o value object
        var rating = Rating.Create(request.Rating);

        // 4. Criar o ProductReview usando o método factory
        var productReview = ProductReview.Create(
            request.ProductId,
            request.UserId,
            rating,
            request.Title,
            request.Comment,
            request.IsVerifiedPurchase);

        // 5. Adicionar ao repositório
        await _productReviewRepository.AddAsync(productReview, cancellationToken);

        _logger.LogInformation("📝 [CreateProductReviewCommandHandler] ProductReview {ProductReviewId} criado para produto {ProductId} pelo usuário {UserId}", 
            productReview.Id, request.ProductId, request.UserId);

        // 6. Persistir mudanças no banco (TransactionBehavior gerencia a transação automaticamente)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Criar resposta de sucesso
        var response = new ProductReviewResponse
        {
            Id = productReview.Id,
            ProductId = productReview.ProductId,
            UserId = productReview.UserId,
            Rating = productReview.Rating.Value,
            Title = productReview.Title,
            Comment = productReview.Comment,
            IsVerifiedPurchase = productReview.IsVerifiedPurchase,
            HelpfulCount = productReview.HelpfulCount,
            UnhelpfulCount = productReview.UnhelpfulCount,
            IsApproved = productReview.IsApproved,
            IsFeatured = productReview.IsFeatured,
            CreatedAt = productReview.CreatedAt,
            UpdatedAt = productReview.UpdatedAt
        };

        _logger.LogInformation("✅ [CreateProductReviewCommandHandler] Processamento concluído com sucesso para CreateProductReviewCommand");
        
        return ApiResponse<ProductReviewResponse>.Ok(response, "Avaliação criada com sucesso.");
    }
}