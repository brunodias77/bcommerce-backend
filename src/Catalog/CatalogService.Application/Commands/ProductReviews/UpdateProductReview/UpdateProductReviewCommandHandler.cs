using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Repository;
using CatalogService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.ProductReviews.UpdateProductReview;

public class UpdateProductReviewCommandHandler : ICommandHandler<UpdateProductReviewCommand, ApiResponse<ProductReviewResponse>>
{
    private readonly IProductReviewRepository _productReviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProductReviewCommandHandler> _logger;

    public UpdateProductReviewCommandHandler(
        IProductReviewRepository productReviewRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateProductReviewCommandHandler> logger)
    {
        _productReviewRepository = productReviewRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<ProductReviewResponse>> HandleAsync(UpdateProductReviewCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("⭐ [UpdateProductReviewCommandHandler] Iniciando processamento para UpdateProductReviewCommand");
        
        // 1. Verificar se a review existe e não foi deletada
        var productReview = await _productReviewRepository.GetByIdAsync(request.Id, cancellationToken);
        if (productReview == null || productReview.DeletedAt.HasValue)
        {
            throw new DomainException("Avaliação não encontrada ou foi removida.");
        }

        // 2. Verificar se o usuário é o dono da review (assumindo que UserId vem do contexto)
        // Nota: Em um cenário real, o UserId viria do contexto de autenticação
        // Por enquanto, vamos assumir que a verificação de propriedade é feita no controller ou middleware
        
        // 3. Criar o Rating usando o value object
        var rating = Rating.Create(request.Rating);

        // 4. Atualizar a review usando o método Update do domain
        productReview.Update(
            rating,
            request.Title,
            request.Comment,
            request.IsVerifiedPurchase);

        _logger.LogInformation("📝 [UpdateProductReviewCommandHandler] ProductReview {ProductReviewId} atualizada", 
            productReview.Id);

        // 5. Persistir mudanças no banco (TransactionBehavior gerencia a transação automaticamente)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Criar resposta de sucesso
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

        _logger.LogInformation("✅ [UpdateProductReviewCommandHandler] Processamento concluído com sucesso para UpdateProductReviewCommand");
        
        return ApiResponse<ProductReviewResponse>.Ok(response, "Avaliação atualizada com sucesso.");
    }
}