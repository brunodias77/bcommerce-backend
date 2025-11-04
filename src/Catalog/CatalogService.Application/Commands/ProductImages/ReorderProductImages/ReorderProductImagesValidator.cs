using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.ProductImages.ReorderProductImages;

/// <summary>
/// Validador para o comando de reordenar imagens de produto
/// </summary>
public class ReorderProductImagesValidator : IValidator<ReorderProductImagesCommand>
{
    /// <summary>
    /// Valida o comando de reordenar imagens
    /// </summary>
    /// <param name="request">Comando a ser validado</param>
    /// <returns>Handler de validação com os erros encontrados</returns>
    public ValidationHandler Validate(ReorderProductImagesCommand request)
    {
        var handler = new ValidationHandler();
        
        // Validar ProductId
        if (request.ProductId == Guid.Empty)
            handler.Add("ID do produto é obrigatório");
        
        // Validar ImageOrders
        if (request.ImageOrders == null || !request.ImageOrders.Any())
        {
            handler.Add("Lista de imagens para reordenação é obrigatória e não pode estar vazia");
        }
        else
        {
            // Validar cada ImageOrder
            for (int i = 0; i < request.ImageOrders.Count; i++)
            {
                var imageOrder = request.ImageOrders[i];
                
                // Validar ImageId
                if (imageOrder.ImageId == Guid.Empty)
                    handler.Add($"ID da imagem na posição {i + 1} é obrigatório");
                
                // Validar DisplayOrder
                if (imageOrder.DisplayOrder < 0)
                    handler.Add($"Ordem de exibição da imagem na posição {i + 1} deve ser maior ou igual a zero");
            }
            
            // Validar IDs duplicados
            var duplicateIds = request.ImageOrders
                .GroupBy(io => io.ImageId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
                
            if (duplicateIds.Any())
            {
                handler.Add($"IDs de imagem duplicados encontrados: {string.Join(", ", duplicateIds)}");
            }
        }
        
        return handler;
    }
}