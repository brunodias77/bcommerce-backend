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
        _logger.LogInformation("⭐ [ApproveProductReviewCommandHandler] Iniciando processamento para ReviewId: {ReviewId}, ModeratorId: {ModeratorId}", 
            request.Id, request.ModeratorId);
        
        // 1. Buscar a avaliação pelo ID
        var productReview = await _productReviewRepository.GetByIdAsync(request.Id, cancellationToken);
        if (productReview == null)
        {
            throw new KeyNotFoundException($"Avaliação com ID {request.Id} não foi encontrada.");
        }

        // 2. Aplicar a aprovação usando o método do domínio
        // O método Approve já faz todas as validações necessárias:
        // - Verifica se foi deletada
        // - Verifica se já está aprovada
        // - Valida o moderatorId
        productReview.Approve(request.ModeratorId);

        // 3. Atualizar no repositório
        _productReviewRepository.Update(productReview);

        _logger.LogInformation("📝 [ApproveProductReviewCommandHandler] Avaliação {ReviewId} aprovada pelo moderador {ModeratorId}", 
            request.Id, request.ModeratorId);

        // 4. Persistir mudanças (TransactionBehavior gerencia a transação automaticamente)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Retornar resposta de sucesso
        _logger.LogInformation("✅ [ApproveProductReviewCommandHandler] Processamento concluído com sucesso para ReviewId: {ReviewId}", 
            request.Id);
        
        return ApiResponse<bool>.Ok(true, "Avaliação aprovada com sucesso.");
    }
}