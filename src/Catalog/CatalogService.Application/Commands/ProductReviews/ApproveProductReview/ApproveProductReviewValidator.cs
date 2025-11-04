using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.ProductReviews.ApproveProductReview;

public class ApproveProductReviewValidator : IValidator<ApproveProductReviewCommand>
{
    public ValidationHandler Validate(ApproveProductReviewCommand command)
    {
        var handler = new ValidationHandler();

        // Validar Id
        if (command.Id == Guid.Empty)
            handler.Add("ID da avaliação é obrigatório");

        // Validar ModeratorId
        if (command.ModeratorId == Guid.Empty)
            handler.Add("ID do moderador é obrigatório");

        return handler;
    }
}