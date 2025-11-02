using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Validations;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CatalogService.Domain.Aggregates;

public class Category : AggregateRoot
{
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Description { get; private set; }
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; }
    public int DisplayOrder { get; private set; }
    public string Metadata { get; private set; } // JSON
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Category()
    {
        Name = string.Empty;
        Slug = string.Empty;
        Metadata = string.Empty;
    }

    public static Category Create(
        string name,
        string slug,
        string? description = null,
        Guid? parentId = null,
        int displayOrder = 0,
        bool isActive = true,
        string metadata = "{}")
    {
        // 1. Apenas criamos a instância com os dados recebidos.
        return new Category
        {
            Name = name,
            Slug = slug,
            Description = description,
            ParentId = parentId,
            IsActive = isActive,
            DisplayOrder = displayOrder,
            Metadata = metadata,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    }

    /// <summary>
    /// Atualiza os dados da categoria
    /// </summary>
    /// <param name="name">Nome da categoria</param>
    /// <param name="slug">Slug da categoria</param>
    /// <param name="description">Descrição da categoria</param>
    /// <param name="parentId">ID da categoria pai</param>
    /// <param name="displayOrder">Ordem de exibição</param>
    /// <param name="isActive">Se a categoria está ativa</param>
    /// <param name="metadata">Metadados em JSON</param>
    public void Update(
        string name,
        string slug,
        string? description = null,
        Guid? parentId = null,
        int displayOrder = 0,
        bool isActive = true,
        string metadata = "{}")
    {
        Name = name;
        Slug = slug;
        Description = description;
        ParentId = parentId;
        DisplayOrder = displayOrder;
        IsActive = isActive;
        Metadata = metadata;
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }

    public override ValidationHandler Validate(ValidationHandler handler)
    {
        // Validar Name (movido do Create para cá)
        if (string.IsNullOrWhiteSpace(Name))
            handler.Add("Nome da categoria é obrigatório");
        else if (Name.Length > 200)
            handler.Add("Nome da categoria deve ter no máximo 200 caracteres");

        // Validar Slug (movido do Create para cá)
        if (string.IsNullOrWhiteSpace(Slug))
            handler.Add("Slug da categoria é obrigatório");
        else if (Slug.Length > 200)
            handler.Add("Slug da categoria deve ter no máximo 200 caracteres");
        else if (!IsValidSlug(Slug))
            handler.Add("Slug deve conter apenas letras minúsculas, números e hífens, sem espaços ou caracteres especiais");

        // Validar Description
        if (!string.IsNullOrEmpty(Description) && Description.Length > 1000)
            handler.Add("Descrição da categoria deve ter no máximo 1000 caracteres");

        // Validar DisplayOrder (movido do Create para cá)
        if (DisplayOrder < 0)
            handler.Add("Ordem de exibição deve ser maior ou igual a zero");

        // Validar Metadata
        if (string.IsNullOrWhiteSpace(Metadata))
            handler.Add("Metadata da categoria é obrigatório"); // Geralmente é opcional, mas mantendo sua regra.
        else if (!IsValidJson(Metadata))
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

    /// <summary>
    /// Realiza o soft delete da categoria
    /// </summary>
    /// <returns>Categoria com soft delete aplicado</returns>
    public Category SoftDelete()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        Version++;
        
        return this;
    }

    /// <summary>
    /// Ativa a categoria (desfaz soft delete)
    /// </summary>
    /// <returns>Categoria ativada</returns>
    public Category Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        Version++;
        
        return this;
    }
}