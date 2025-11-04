using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.ProductReviews.ApproveProductReview;

public class ApproveProductReviewCommandHandler : ICommandHandler<ApproveProductReviewCommand, ApiResponse<bool>>
{
    private readonly IProductReviewRepository _productReviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveProductReviewCommandHandler> _logger;

    public ApproveProductReviewCommandHandler(
        IProductReviewRepository productReviewRepository,
        IUnitOfWork unitOfWork,
        ILogger<ApproveProductReviewCommandHandler> logger)
    {
        _productReviewRepository = productReviewRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> HandleAsync(ApproveProductReviewCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("✅ [ApproveProductReviewCommandHandler] Iniciando processamento para ApproveProductReviewCommand");

        // 1. Verificar se a review existe e não foi deletada
        var productReview = await _productReviewRepository.GetByIdAsync(request.Id, cancellationToken);
        if (productReview == null || productReview.DeletedAt.HasValue)
        {
            throw new DomainException("Avaliação não encontrada ou foi removida.");
        }

        // 2. Aprovar a avaliação
        productReview.Approve(request.ModeratorId);

        _logger.LogInformation("📝 [ApproveProductReviewCommandHandler] ProductReview {ProductReviewId} aprovada por moderador {ModeratorId}",
            productReview.Id, request.ModeratorId);

        // 3. Persistir mudanças no banco
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✅ [ApproveProductReviewCommandHandler] Processamento concluído com sucesso para ApproveProductReviewCommand");

        return ApiResponse<bool>.Ok(true, "Avaliação aprovada com sucesso.");
    }
}

