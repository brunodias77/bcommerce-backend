using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.ProductReviews.UpdateProductReview;

public class UpdateProductReviewValidator : IValidator<UpdateProductReviewCommand>
{
    public ValidationHandler Validate(UpdateProductReviewCommand command)
    {
        var handler = new ValidationHandler();
        
        // Validar Id
        if (command.Id == Guid.Empty)
            handler.Add("ID da avaliação é obrigatório");
        
        // Validar Rating
        if (command.Rating < 1 || command.Rating > 5)
            handler.Add("Avaliação deve estar entre 1 e 5 estrelas");
        
        // Validar Title (opcional, mas se fornecido deve ter tamanho válido)
        if (!string.IsNullOrEmpty(command.Title))
        {
            if (string.IsNullOrWhiteSpace(command.Title))
                handler.Add("Título não pode conter apenas espaços em branco");
            
            if (command.Title.Length > 100)
                handler.Add("Título deve ter no máximo 100 caracteres");
        }
        
        // Validar Comment (opcional, mas se fornecido deve ter tamanho válido)
        if (!string.IsNullOrEmpty(command.Comment))
        {
            if (string.IsNullOrWhiteSpace(command.Comment))
                handler.Add("Comentário não pode conter apenas espaços em branco");
            
            if (command.Comment.Length > 2000)
                handler.Add("Comentário deve ter no máximo 2000 caracteres");
        }
        
        return handler;
    }
}