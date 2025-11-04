using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.ProductReviews.CreateProductReview;

public class CreateProductReviewValidator : IValidator<CreateProductReviewCommand>
{
    public ValidationHandler Validate(CreateProductReviewCommand command)
    {
        var handler = new ValidationHandler();
        
        // Validar ProductId
        if (command.ProductId == Guid.Empty)
            handler.Add("ID do produto é obrigatório");
        
        // Validar UserId
        if (command.UserId == Guid.Empty)
            handler.Add("ID do usuário é obrigatório");
        
        // Validar Rating
        if (command.Rating < 1 || command.Rating > 5)
            handler.Add("Nota deve estar entre 1 e 5");
        
        // Validar Title (opcional)
        if (!string.IsNullOrEmpty(command.Title) && command.Title.Length > 100)
            handler.Add("Título da avaliação deve ter no máximo 100 caracteres");
        
        // Validar Comment (opcional)
        if (!string.IsNullOrEmpty(command.Comment) && command.Comment.Length > 2000)
            handler.Add("Comentário deve ter no máximo 2000 caracteres");
        
        return handler;
    }
}