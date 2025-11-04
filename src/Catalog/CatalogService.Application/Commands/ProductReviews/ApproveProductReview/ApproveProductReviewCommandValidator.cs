using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.ProductReviews.ApproveProductReview;

public class ApproveProductReviewCommandValidator : IValidator<ApproveProductReviewCommand>
{
    public ValidationHandler Validate(ApproveProductReviewCommand request)
    {
        var handler = new ValidationHandler();
        
        // Validar Id da avaliação
        if (request.Id == Guid.Empty)
            handler.Add("ID da avaliação é obrigatório");
        
        // Validar ModeratorId
        if (request.ModeratorId == Guid.Empty)
            handler.Add("ID do moderador é obrigatório");
        
        return handler;
    }
}