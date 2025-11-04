# 📋 Especificação Técnica - AddProductToFavorites Command

## 1. Visão Geral

**Comando**: AddProductToFavorites  
**Status**: Pendente → Em Implementação  
**Descrição**: Adiciona um produto aos favoritos do usuário  
**Localização**: `/src/Catalog/CatalogService.Application/Commands/FavoriteProducts/AddProductToFavorites/`

## 2. Análise dos Padrões do Projeto

### 2.1 Estrutura de Comandos Existentes
A análise dos comandos implementados revela o seguinte padrão consistente:

1. **Command**: Define os parâmetros de entrada implementando `ICommand<TResponse>`
2. **CommandHandler**: Processa a lógica de negócio implementando `ICommandHandler<TCommand, TResponse>`
3. **CommandValidator**: Valida os dados de entrada implementando `IValidator<TCommand>`
4. **CommandResponse**: Define a estrutura de resposta

### 2.2 Padrões Identificados
- **CQRS**: Separação clara entre comandos e queries
- **Response Pattern**: Uso consistente de `ApiResponse<T>` para respostas
- **Validação**: Validação automática via `ValidationBehavior`
- **Logs**: Logs estruturados com emojis e identificadores
- **Transações**: Gerenciamento automático via `TransactionBehavior`
- **Exceções**: Uso de `DomainException` e `KeyNotFoundException`

## 3. Estrutura do Comando AddProductToFavorites

### 3.1 AddProductToFavoritesCommand
```csharp
using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.FavoriteProducts.AddProductToFavorites;

public class AddProductToFavoritesCommand : ICommand<ApiResponse<bool>>
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
}
```

### 3.2 AddProductToFavoritesResponse
```csharp
namespace CatalogService.Application.Commands.FavoriteProducts.AddProductToFavorites;

public class AddProductToFavoritesResponse
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime FavoritedAt { get; set; }
    public int TotalFavorites { get; set; } // Contador total de favoritos do produto
}
```

### 3.3 AddProductToFavoritesCommandValidator
```csharp
using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.FavoriteProducts.AddProductToFavorites;

public class AddProductToFavoritesCommandValidator : IValidator<AddProductToFavoritesCommand>
{
    public ValidationHandler Validate(AddProductToFavoritesCommand request)
    {
        var handler = new ValidationHandler();
        
        // Validar UserId
        if (request.UserId == Guid.Empty)
            handler.Add("ID do usuário é obrigatório");
        
        // Validar ProductId
        if (request.ProductId == Guid.Empty)
            handler.Add("ID do produto é obrigatório");
        
        return handler;
    }
}
```

## 4. Implementação do Handler

### 4.1 AddProductToFavoritesCommandHandler
```csharp
using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.FavoriteProducts.AddProductToFavorites;

public class AddProductToFavoritesCommandHandler : 
    ICommandHandler<AddProductToFavoritesCommand, ApiResponse<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly IFavoriteProductRepository _favoriteProductRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddProductToFavoritesCommandHandler> _logger;

    public AddProductToFavoritesCommandHandler(
        IProductRepository productRepository,
        IFavoriteProductRepository favoriteProductRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddProductToFavoritesCommandHandler> logger)
    {
        _productRepository = productRepository;
        _favoriteProductRepository = favoriteProductRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> HandleAsync(
        AddProductToFavoritesCommand request, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("❤️ [AddProductToFavoritesCommandHandler] Iniciando processamento para UserId: {UserId}, ProductId: {ProductId}", 
            request.UserId, request.ProductId);
        
        // 1. Verificar se o produto existe e está ativo
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Produto com ID {request.ProductId} não foi encontrado.");
        }

        // 2. Verificar se o produto não está deletado
        if (product.DeletedAt.HasValue)
        {
            throw new DomainException("Não é possível favoritar um produto deletado.");
        }

        // 3. Verificar se o produto está ativo
        if (!product.IsActive)
        {
            throw new DomainException("Não é possível favoritar um produto inativo.");
        }

        // 4. Verificar se já existe favorito para este usuário/produto
        var existingFavorite = await _favoriteProductRepository.FindAsync(
            f => f.UserId == request.UserId && f.ProductId == request.ProductId, 
            cancellationToken);
        
        if (existingFavorite.Any())
        {
            throw new DomainException("Produto já está nos favoritos do usuário.");
        }

        // 5. Criar o favorito
        var favoriteProduct = FavoriteProduct.Create(request.UserId, request.ProductId);
        
        // 6. Adicionar ao repositório
        await _favoriteProductRepository.AddAsync(favoriteProduct, cancellationToken);

        // 7. Atualizar contador de favoritos no produto
        product.IncrementFavoriteCount();
        _productRepository.Update(product);

        // 8. Persistir mudanças (TransactionBehavior gerencia a transação)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✅ [AddProductToFavoritesCommandHandler] Favorito adicionado com sucesso para UserId: {UserId}, ProductId: {ProductId}", 
            request.UserId, request.ProductId);
        
        return ApiResponse<bool>.Ok(true, "Produto adicionado aos favoritos com sucesso.");
    }
}
```

## 5. Método Adicional na Entidade Product

### 5.1 IncrementFavoriteCount() em Product.cs
```csharp
public void IncrementFavoriteCount()
{
    FavoriteCount++;
    UpdatedAt = DateTime.UtcNow;
    Version++;
}

public void DecrementFavoriteCount()
{
    if (FavoriteCount > 0)
        FavoriteCount--;
    UpdatedAt = DateTime.UtcNow;
    Version++;
}
```

## 6. Endpoint HTTP no ProductController

### 6.1 Método AddToFavorites
```csharp
/// <summary>
/// Adiciona um produto aos favoritos do usuário
/// </summary>
/// <param name="productId">ID do produto a ser favoritado</param>
/// <param name="userId">ID do usuário (via header ou claim)</param>
/// <param name="cancellationToken">Token de cancelamento</param>
/// <returns>Confirmação da operação</returns>
/// <response code="200">Produto favoritado com sucesso</response>
/// <response code="400">Dados inválidos ou erro de validação</response>
/// <response code="404">Produto não encontrado</response>
/// <response code="409">Produto já está nos favoritos ou está deletado/inativo</response>
/// <response code="500">Erro interno do servidor</response>
[HttpPost("{productId:guid}/favorites")]
[ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> AddToFavorites([FromRoute] Guid productId, 
    [FromHeader(Name = "X-User-Id")] Guid userId,
    CancellationToken cancellationToken = default)
{
    _logger.LogInformation("❤️ [ProductController] Iniciando AddProductToFavoritesCommand para ProductId: {ProductId}, UserId: {UserId}", 
        productId, userId);

    var command = new AddProductToFavoritesCommand
    {
        ProductId = productId,
        UserId = userId
    };

    // Validar ModelState
    if (!ModelState.IsValid)
    {
        var errors = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => new Error(e.ErrorMessage))
            .ToList();

        throw new ValidationException(errors);
    }

    // Enviar command via Mediator
    var result = await _mediator.SendAsync<ApiResponse<bool>>(command, cancellationToken);

    _logger.LogInformation("✅ [ProductController] Operação concluída com sucesso para AddProductToFavoritesCommand");

    return Ok(result);
}
```

## 7. Registro de Dependências

### 7.1 Em ApplicationDependencyInjection.cs
```csharp
// Adicionar ao método AddMediator
services.AddScoped<IValidator<AddProductToFavoritesCommand>, AddProductToFavoritesCommandValidator>();
services.AddScoped<ICommandHandler<AddProductToFavoritesCommand, ApiResponse<bool>>, AddProductToFavoritesCommandHandler>();
```

### 7.2 Using statements no ProductController
```csharp
using CatalogService.Application.Commands.FavoriteProducts.AddProductToFavorites;
```

## 8. Estrutura de Diretórios

```
CatalogService.Application/
├── Commands/
│   ├── FavoriteProducts/
│   │   └── AddProductToFavorites/
│   │       ├── AddProductToFavoritesCommand.cs
│   │       ├── AddProductToFavoritesCommandHandler.cs
│   │       ├── AddProductToFavoritesCommandValidator.cs
│   │       └── AddProductToFavoritesResponse.cs
```

## 9. Validações e Regras de Negócio

### 9.1 Validações de Entrada
- UserId não pode ser Guid.Empty
- ProductId não pode ser Guid.Empty

### 9.2 Regras de Negócio
- Produto deve existir
- Produto não pode estar deletado (DeletedAt.HasValue)
- Produto deve estar ativo (IsActive = true)
- Usuário não pode favoritar o mesmo produto duas vezes
- Contador FavoriteCount deve ser incrementado automaticamente

## 10. Tratamento de Exceções

| Exceção | Código HTTP | Mensagem |
|---------|-------------|----------|
| KeyNotFoundException | 404 | Produto com ID {id} não foi encontrado |
| DomainException | 409 | Produto já está nos favoritos / Produto deletado / Produto inativo |
| ValidationException | 400 | Erros de validação do ModelState |

## 11. Logs e Monitoramento

- **Início**: `❤️ [AddProductToFavoritesCommandHandler] Iniciando processamento para UserId: {UserId}, ProductId: {ProductId}`
- **Sucesso**: `✅ [AddProductToFavoritesCommandHandler] Favorito adicionado com sucesso para UserId: {UserId}, ProductId: {ProductId}`
- **Controller**: Logs similares com prefixo `[ProductController]`

## 12. Considerações de Performance

- Verificação de existência via `GetByIdAsync` (índice em ID)
- Verificação de duplicado via `FindAsync` com filtro combinado (recomenda-se índice composto)
- Atualização de contador via método específico na entidade
- Transação automática via `TransactionBehavior`

## 13. Testes Recomendados

1. **Adicionar favorito com sucesso**
2. **Tentar favoritar produto inexistente (404)**
3. **Tentar favoritar produto deletado (409)**
4. **Tentar favoritar produto inativo (409)**
5. **Tentar favoritar mesmo produto duas vezes (409)**
6. **Validar incremento do contador FavoriteCount**
7. **Validar dados inválidos (400)**
8. **Verificar criação da entidade FavoriteProduct**

## 14. Próximos Passos

1. Implementar comando `RemoveProductFromFavorites`
2. Implementar queries `GetUserFavoriteProducts` e `CheckIfProductIsFavorited`
3. Adicionar eventos de domínio se necessário
4. Considerar cache para consultas de favoritos
5. Implementar paginação para listagem de favoritos

---

**Data da Especificação**: [Data Atual]  
**Versão**: 1.0  
**Status**: Pronto para Implementação