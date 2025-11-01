using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Validations;

namespace CatalogService.Domain.Entities;

public class ProductImage : Entity
{
    public Guid ProductId { get; private set; }
    public string Url { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public string? AltText { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ProductImage() 
    {
        Url = string.Empty;
    }

    public static ProductImage Create(
        Guid productId, 
        string url, 
        string? thumbnailUrl = null, 
        string? altText = null, 
        int displayOrder = 0, 
        bool isPrimary = false)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty", nameof(productId));

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url is required", nameof(url));

        if (displayOrder < 0)
            throw new ArgumentException("DisplayOrder cannot be negative", nameof(displayOrder));

        var productImage = new ProductImage
        {
            ProductId = productId,
            Url = url,
            ThumbnailUrl = thumbnailUrl,
            AltText = altText,
            DisplayOrder = displayOrder,
            IsPrimary = isPrimary,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var validationResult = productImage.Validate();
        if (validationResult.HasErrors)
        {
            throw new ArgumentException($"Dados inválidos: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
        }

        return productImage;
    }

    public override ValidationHandler Validate()
    {
        var handler = new ValidationHandler();
        
        // Validar ProductId
        if (ProductId == Guid.Empty)
            handler.Add("ID do produto é obrigatório");
        
        // Validar Url
        if (string.IsNullOrEmpty(Url))
            handler.Add("URL da imagem é obrigatória");
        else if (string.IsNullOrWhiteSpace(Url))
            handler.Add("URL da imagem não pode conter apenas espaços em branco");
        else if (Url.Length > 2000)
            handler.Add("URL da imagem deve ter no máximo 2000 caracteres");
        else if (!IsValidUrl(Url))
            handler.Add("URL da imagem deve ser uma URL válida (HTTP/HTTPS)");
        
        // Validar ThumbnailUrl (opcional)
        if (!string.IsNullOrEmpty(ThumbnailUrl))
        {
            if (string.IsNullOrWhiteSpace(ThumbnailUrl))
                handler.Add("URL da miniatura não pode conter apenas espaços em branco");
            else if (ThumbnailUrl.Length > 2000)
                handler.Add("URL da miniatura deve ter no máximo 2000 caracteres");
            else if (!IsValidUrl(ThumbnailUrl))
                handler.Add("URL da miniatura deve ser uma URL válida (HTTP/HTTPS)");
        }
        
        // Validar AltText (opcional)
        if (!string.IsNullOrEmpty(AltText))
        {
            if (string.IsNullOrWhiteSpace(AltText))
                handler.Add("Texto alternativo não pode conter apenas espaços em branco");
            else if (AltText.Length < 3)
                handler.Add("Texto alternativo deve ter no mínimo 3 caracteres");
            else if (AltText.Length > 500)
                handler.Add("Texto alternativo deve ter no máximo 500 caracteres");
        }
        
        // Validar DisplayOrder
        if (DisplayOrder < 0)
            handler.Add("Ordem de exibição deve ser maior ou igual a zero");
        
        // Validar CreatedAt
        if (CreatedAt == default(DateTime))
            handler.Add("Data de criação é obrigatória");
        else if (CreatedAt > DateTime.UtcNow.AddMinutes(1))
            handler.Add("Data de criação não pode estar no futuro");
        
        // Validar UpdatedAt
        if (UpdatedAt == default(DateTime))
            handler.Add("Data de atualização é obrigatória");
        else if (UpdatedAt > DateTime.UtcNow.AddMinutes(1))
            handler.Add("Data de atualização não pode estar no futuro");
        
        // Validar relação entre CreatedAt e UpdatedAt
        if (CreatedAt != default(DateTime) && UpdatedAt != default(DateTime) && UpdatedAt < CreatedAt)
            handler.Add("Data de atualização deve ser maior ou igual à data de criação");
        
        return handler;
    }
    
    private static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;
            
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) 
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}