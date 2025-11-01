using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;
using System.Text.RegularExpressions;

namespace CatalogService.Application.Commands.Products.CreateProduct;

public class CreateProductCommandValidator : IValidator<CreateProductCommand>
{
    public ValidationHandler Validate(CreateProductCommand command)
    {
        var handler = new ValidationHandler();
        
        // Validar Name
        if (string.IsNullOrWhiteSpace(command.Name))
            handler.Add("Nome do produto é obrigatório");
        else if (command.Name.Length > 200)
            handler.Add("Nome do produto deve ter no máximo 200 caracteres");
        
        // Validar Slug
        if (string.IsNullOrWhiteSpace(command.Slug))
            handler.Add("Slug do produto é obrigatório");
        else if (command.Slug.Length > 200)
            handler.Add("Slug do produto deve ter no máximo 200 caracteres");
        else if (!IsValidSlug(command.Slug))
            handler.Add("Slug deve conter apenas letras minúsculas, números e hífens, sem espaços ou caracteres especiais");
        
        // Validar Description (opcional)
        if (!string.IsNullOrEmpty(command.Description) && command.Description.Length > 2000)
            handler.Add("Descrição do produto deve ter no máximo 2000 caracteres");
        
        // Validar ShortDescription (opcional)
        if (!string.IsNullOrEmpty(command.ShortDescription) && command.ShortDescription.Length > 500)
            handler.Add("Descrição curta do produto deve ter no máximo 500 caracteres");
        
        // Validar Price
        if (command.Price <= 0)
            handler.Add("Preço do produto deve ser maior que zero");
        
        // Validar Currency
        if (string.IsNullOrWhiteSpace(command.Currency))
            handler.Add("Moeda é obrigatória");
        else if (command.Currency.Length != 3)
            handler.Add("Moeda deve ter exatamente 3 caracteres (ex: BRL, USD)");
        
        // Validar CompareAtPrice (opcional)
        if (command.CompareAtPrice.HasValue && command.CompareAtPrice.Value <= command.Price)
            handler.Add("Preço de comparação deve ser maior que o preço do produto");
        
        // Validar CostPrice (opcional)
        if (command.CostPrice.HasValue && command.CostPrice.Value <= 0)
            handler.Add("Preço de custo deve ser maior que zero");
        
        // Validar Stock
        if (command.Stock < 0)
            handler.Add("Estoque deve ser maior ou igual a zero");
        
        // Validar LowStockThreshold
        if (command.LowStockThreshold < 0)
            handler.Add("Limite de estoque baixo deve ser maior ou igual a zero");
        
        // Validar MetaTitle (opcional)
        if (!string.IsNullOrEmpty(command.MetaTitle) && command.MetaTitle.Length > 60)
            handler.Add("Meta título deve ter no máximo 60 caracteres");
        
        // Validar MetaDescription (opcional)
        if (!string.IsNullOrEmpty(command.MetaDescription) && command.MetaDescription.Length > 160)
            handler.Add("Meta descrição deve ter no máximo 160 caracteres");
        
        // Validar WeightKg (opcional)
        if (command.WeightKg.HasValue && command.WeightKg.Value <= 0)
            handler.Add("Peso deve ser maior que zero");
        
        // Validar Sku (opcional)
        if (!string.IsNullOrEmpty(command.Sku) && command.Sku.Length > 50)
            handler.Add("SKU deve ter no máximo 50 caracteres");
        
        // Validar Barcode (opcional)
        if (!string.IsNullOrEmpty(command.Barcode) && command.Barcode.Length > 50)
            handler.Add("Código de barras deve ter no máximo 50 caracteres");
        
        return handler;
    }
    
    private static bool IsValidSlug(string slug)
    {
        // Slug deve conter apenas letras minúsculas, números e hífens
        // Não pode começar ou terminar com hífen
        var slugPattern = @"^[a-z0-9]+(?:-[a-z0-9]+)*$";
        return Regex.IsMatch(slug, slugPattern);
    }
}