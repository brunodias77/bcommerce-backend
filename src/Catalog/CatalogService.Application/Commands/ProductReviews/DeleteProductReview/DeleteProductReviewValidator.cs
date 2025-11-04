using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.ProductReviews.DeleteProductReview;

public class DeleteProductReviewValidator : IValidator<DeleteProductReviewCommand>
{
    public ValidationHandler Validate(DeleteProductReviewCommand command)
    {
        var handler = new ValidationHandler();
        
        // Validar ID obrigatório
        if (command.Id == Guid.Empty)
            handler.Add("ID da avaliação é obrigatório");
        
        return handler;
    }
}