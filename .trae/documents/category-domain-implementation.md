# Implementação de Domínio - Category Entity

## Value Objects

### Slug Value Object
```csharp
// Slug.cs
using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Exceptions;

namespace CatalogService.Domain.Categories.ValueObjects;

public class Slug : ValueObject
{
    public string Value { get; private set; }

    private Slug() { } // EF Constructor

    public Slug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Slug não pode ser vazio");

        if (value.Length > 120)
            throw new DomainException("Slug deve ter no máximo 120 caracteres");

        if (!IsValidSlug(value))
            throw new DomainException("Slug deve conter apenas letras minúsculas, números e hífens");

        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static bool IsValidSlug(string slug)
    {
        return slug.All(c => char.IsLower(c) || char.IsDigit(c) || c == '-') &&
               !slug.StartsWith('-') && !slug.EndsWith('-') &&
               !slug.Contains("--");
    }

    public static implicit operator string(Slug slug) => slug.Value;
    public static explicit operator Slug(string value) => new(value);

    public override string ToString() => Value;
}
```

### CategoryMetadata Value Object
```csharp
// CategoryMetadata.cs
using BuildingBlocks.Core.Domain;
using System.Text.Json;

namespace CatalogService.Domain.Categories.ValueObjects;

public class CategoryMetadata : ValueObject
{
    public Dictionary<string, object> Data { get; private set; }

    private CategoryMetadata() { Data = new Dictionary<string, object>(); }

    public CategoryMetadata(Dictionary<string, object> data)
    {
        Data = data ?? new Dictionary<string, object>();
    }

    public static CategoryMetadata Empty() => new();

    public static CategoryMetadata FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Empty();

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json) 
                      ?? new Dictionary<string, object>();
            return new CategoryMetadata(data);
        }
        catch (JsonException)
        {
            return Empty();
        }
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(Data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public T? GetValue<T>(string key)
    {
        if (!Data.ContainsKey(key))
            return default;

        try
        {
            var jsonElement = (JsonElement)Data[key];
            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
        }
        catch
        {
            return default;
        }
    }

    public CategoryMetadata SetValue(string key, object value)
    {
        var newData = new Dictionary<string, object>(Data)
        {
            [key] = value
        };
        return new CategoryMetadata(newData);
    }

    public CategoryMetadata RemoveValue(string key)
    {
        var newData = new Dictionary<string, object>(Data);
        newData.Remove(key);
        return new CategoryMetadata(newData);
    }

    public bool HasValue(string key) => Data.ContainsKey(key);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var kvp in Data.OrderBy(x => x.Key))
        {
            yield return kvp.Key;
            yield return kvp.Value;
        }
    }
}
```

## Domain Events

### CategoryCreatedEvent
```csharp
// CategoryCreatedEvent.cs
using BuildingBlocks.CQRS.Events;

namespace CatalogService.Domain.Categories.Events;

public record CategoryCreatedEvent(
    Guid CategoryId,
    string Name,
    string Slug
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

### CategoryUpdatedEvent
```csharp
// CategoryUpdatedEvent.cs
using BuildingBlocks.CQRS.Events;

namespace CatalogService.Domain.Categories.Events;

public record CategoryUpdatedEvent(
    Guid CategoryId,
    string Name,
    string Slug
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

### CategoryActivatedEvent
```csharp
// CategoryActivatedEvent.cs
using BuildingBlocks.CQRS.Events;

namespace CatalogService.Domain.Categories.Events;

public record CategoryActivatedEvent(
    Guid CategoryId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

### CategoryDeactivatedEvent
```csharp
// CategoryDeactivatedEvent.cs
using BuildingBlocks.CQRS.Events;

namespace CatalogService.Domain.Categories.Events;

public record CategoryDeactivatedEvent(
    Guid CategoryId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

## Repository Interface

### ICategoryRepository
```csharp
// ICategoryRepository.cs
using BuildingBlocks.Core.Data;
using CatalogService.Domain.Categories.ValueObjects;

namespace CatalogService.Domain.Categories;

public interface ICategoryRepository : IRepository<Category>
{
    Task<Category?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetByParentIdAsync(Guid? parentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetHierarchyAsync(CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(Slug slug, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasChildrenAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<int> GetMaxDisplayOrderAsync(Guid? parentId, CancellationToken cancellationToken = default);
}
```

## Domain Services

### CategoryDomainService
```csharp
// CategoryDomainService.cs
using BuildingBlocks.Core.Exceptions;
using CatalogService.Domain.Categories.ValueObjects;

namespace CatalogService.Domain.Categories.Services;

public class CategoryDomainService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryDomainService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<string> GenerateUniqueSlugAsync(string name, Guid? excludeId = null, 
                                                     CancellationToken cancellationToken = default)
    {
        var baseSlug = GenerateSlugFromName(name);
        var slug = baseSlug;
        var counter = 1;

        while (await _categoryRepository.SlugExistsAsync(new Slug(slug), excludeId, cancellationToken))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    public async Task ValidateHierarchyAsync(Guid categoryId, Guid? newParentId, 
                                           CancellationToken cancellationToken = default)
    {
        if (newParentId == null)
            return;

        if (newParentId == categoryId)
            throw new DomainException("Uma categoria não pode ser pai de si mesma");

        // Verificar se não criará referência circular
        await ValidateCircularReferenceAsync(categoryId, newParentId.Value, cancellationToken);
    }

    public async Task ValidateDeleteAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var hasChildren = await _categoryRepository.HasChildrenAsync(categoryId, cancellationToken);
        if (hasChildren)
            throw new DomainException("Não é possível excluir uma categoria que possui subcategorias");
    }

    private async Task ValidateCircularReferenceAsync(Guid categoryId, Guid parentId, 
                                                     CancellationToken cancellationToken)
    {
        var visited = new HashSet<Guid> { categoryId };
        var currentParentId = parentId;

        while (currentParentId != null)
        {
            if (visited.Contains(currentParentId.Value))
                throw new DomainException("A operação criaria uma referência circular na hierarquia");

            visited.Add(currentParentId.Value);

            var parent = await _categoryRepository.GetByIdAsync(currentParentId.Value, cancellationToken);
            currentParentId = parent?.ParentId;
        }
    }

    private static string GenerateSlugFromName(string name)
    {
        return name.ToLowerInvariant()
                  .Replace(" ", "-")
                  .Replace("ã", "a").Replace("á", "a").Replace("à", "a").Replace("â", "a")
                  .Replace("é", "e").Replace("ê", "e")
                  .Replace("í", "i")
                  .Replace("ó", "o").Replace("ô", "o").Replace("õ", "o")
                  .Replace("ú", "u").Replace("ü", "u")
                  .Replace("ç", "c")
                  .Replace("ñ", "n")
                  .Where(c => char.IsLetterOrDigit(c) || c == '-')
                  .Aggregate("", (current, c) => current + c)
                  .Trim('-');
    }
}
```

## Especificações de Domínio

### CategorySpecifications
```csharp
// CategorySpecifications.cs
using System.Linq.Expressions;

namespace CatalogService.Domain.Categories.Specifications;

public static class CategorySpecifications
{
    public static Expression<Func<Category, bool>> IsActive()
    {
        return category => category.IsActive;
    }

    public static Expression<Func<Category, bool>> HasParent(Guid parentId)
    {
        return category => category.ParentId == parentId;
    }

    public static Expression<Func<Category, bool>> IsRootCategory()
    {
        return category => category.ParentId == null;
    }

    public static Expression<Func<Category, bool>> NameContains(string searchTerm)
    {
        return category => category.Name.ToLower().Contains(searchTerm.ToLower());
    }

    public static Expression<Func<Category, bool>> SlugEquals(string slug)
    {
        return category => category.Slug.Value == slug;
    }
}
```

## Estrutura de Arquivos Recomendada

```
CatalogService.Domain/
├── Categories/
│   ├── Category.cs (Aggregate Root)
│   ├── ICategoryRepository.cs
│   ├── Events/
│   │   ├── CategoryCreatedEvent.cs
│   │   ├── CategoryUpdatedEvent.cs
│   │   ├── CategoryActivatedEvent.cs
│   │   └── CategoryDeactivatedEvent.cs
│   ├── ValueObjects/
│   │   ├── Slug.cs
│   │   └── CategoryMetadata.cs
│   ├── Services/
│   │   └── CategoryDomainService.cs
│   └── Specifications/
│       └── CategorySpecifications.cs
```

Esta implementação segue os princípios de Domain-Driven Design (DDD), garantindo que:

1. **Category** é um Aggregate Root que encapsula regras de negócio
2. **Value Objects** (Slug, CategoryMetadata) garantem imutabilidade e validação
3. **Domain Events** permitem comunicação assíncrona entre bounded contexts
4. **Domain Services** encapsulam lógica de domínio complexa
5. **Specifications** facilitam consultas expressivas e reutilizáveis
6. **Repository Interface** define contratos para persistência sem acoplar à infraestrutura