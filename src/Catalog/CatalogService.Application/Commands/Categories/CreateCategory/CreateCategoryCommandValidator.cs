using BuildingBlocks.Core.Validations;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CatalogService.Application.Commands.Categories.CreateCategory;

public class CreateCategoryCommandValidator
{
    public ValidationHandler Validate(CreateCategoryCommand command)
    {
        var handler = new ValidationHandler();
        
        // Validar Name
        if (string.IsNullOrWhiteSpace(command.Name))
            handler.Add("Nome da categoria é obrigatório");
        else if (command.Name.Length > 200)
            handler.Add("Nome da categoria deve ter no máximo 200 caracteres");
        
        // Validar Slug
        if (string.IsNullOrWhiteSpace(command.Slug))
            handler.Add("Slug da categoria é obrigatório");
        else if (command.Slug.Length > 200)
            handler.Add("Slug da categoria deve ter no máximo 200 caracteres");
        else if (!IsValidSlug(command.Slug))
            handler.Add("Slug deve conter apenas letras minúsculas, números e hífens, sem espaços ou caracteres especiais");
        
        // Validar Description
        if (!string.IsNullOrEmpty(command.Description) && command.Description.Length > 1000)
            handler.Add("Descrição da categoria deve ter no máximo 1000 caracteres");
        
        // Validar DisplayOrder
        if (command.DisplayOrder < 0)
            handler.Add("Ordem de exibição deve ser maior ou igual a zero");
        
        // Validar Metadata
        if (string.IsNullOrWhiteSpace(command.Metadata))
            handler.Add("Metadata da categoria é obrigatório");
        else if (!IsValidJson(command.Metadata))
            handler.Add("Metadata deve ser um JSON válido");
        
        return handler;
    }
    
    private static bool IsValidSlug(string slug)
    {
        // Slug deve conter apenas letras minúsculas, números e hífens
        // Não pode começar ou terminar com hífen
        var slugPattern = @"^[a-z0-9]+(?:-[a-z0-9]+)*$";
        return Regex.IsMatch(slug, slugPattern);
    }
    
    private static bool IsValidJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}