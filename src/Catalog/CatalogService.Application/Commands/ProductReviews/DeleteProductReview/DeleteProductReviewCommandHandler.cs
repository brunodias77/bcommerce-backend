using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.ProductReviews.DeleteProductReview;

public class DeleteProductReviewCommandHandler : ICommandHandler<DeleteProductReviewCommand, ApiResponse<bool>>
{
    private readonly IProductReviewRepository _productReviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteProductReviewCommandHandler> _logger;

    public DeleteProductReviewCommandHandler(
        IProductReviewRepository productReviewRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteProductReviewCommandHandler> logger)
    {
        _productReviewRepository = productReviewRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> HandleAsync(DeleteProductReviewCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗑️ [DeleteProductReviewCommandHandler] Iniciando processamento para DeleteProductReviewCommand");
        
        // 1. Verificar se a review existe e não foi deletada
        var productReview = await _productReviewRepository.GetByIdAsync(request.Id, cancellationToken);
        if (productReview == null || productReview.DeletedAt.HasValue)
        {
            throw new DomainException("Avaliação não encontrada ou já foi removida.");
        }

        // 2. Verificar se o usuário é o dono da review (assumindo que UserId vem do contexto)
        // Nota: Em um cenário real, o UserId viria do contexto de autenticação
        // Por enquanto, vamos assumir que a verificação de propriedade é feita no controller ou middleware
        
        // 3. Realizar soft delete usando o método SoftDelete do domain
        productReview.SoftDelete();

        _logger.LogInformation("🗑️ [DeleteProductReviewCommandHandler] ProductReview {ProductReviewId} marcada como deletada", 
            productReview.Id);

        // 4. Persistir mudanças no banco (TransactionBehavior gerencia a transação automaticamente)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✅ [DeleteProductReviewCommandHandler] Processamento concluído com sucesso para DeleteProductReviewCommand");
        
        return ApiResponse<bool>.Ok(true, "Avaliação removida com sucesso.");
    }
}