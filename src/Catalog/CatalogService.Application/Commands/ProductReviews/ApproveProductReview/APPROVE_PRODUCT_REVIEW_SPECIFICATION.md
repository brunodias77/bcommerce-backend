# Especificação Técnica - Comando ApproveProductReview

## 📋 Visão Geral

**Comando**: `ApproveProductReview`  
**Status**: Pendente → ✅ Implementação Planejada  
**Descrição**: Aprova uma avaliação para publicação  
**Localização**: `/Users/diasbruno/Documents/programacao/codigos/dotnet/bcommerce-backend/src/Catalog/CatalogService.Application/Commands/ProductReviews/ApproveProductReview/`

## 🎯 Objetivo

Implementar o comando `ApproveProductReview` que permite a um moderador aprovar uma avaliação de produto para publicação, seguindo os padrões CQRS, Clean Architecture e as práticas estabelecidas no projeto.

## 📦 Estrutura dos Arquivos

```
ApproveProductReview/
├── ApproveProductReviewCommand.cs
├── ApproveProductReviewCommandHandler.cs
├── ApproveProductReviewCommandValidator.cs
└── ApproveProductReviewResponse.cs
```

## 🔧 Detalhes de Implementação

### 1. ApproveProductReviewCommand

**Interface**: `ICommand<ApiResponse<bool>>`  
**Namespace**: `CatalogService.Application.Commands.ProductReviews.ApproveProductReview`

```csharp
public class ApproveProductReviewCommand : ICommand<ApiResponse<bool>>
{
    public Guid Id { get; set; }              // ID da avaliação
    public Guid ModeratorId { get; set; }   // ID do moderador
}
```

### 2. ApproveProductReviewCommandHandler

**Interface**: `ICommandHandler<ApproveProductReviewCommand, ApiResponse<bool>>`  
**Namespace**: `CatalogService.Application.Commands.ProductReviews.ApproveProductReview`

**Dependências**:
- `IProductReviewRepository _productReviewRepository`
- `IUnitOfWork _unitOfWork`
- `ILogger<ApproveProductReviewCommandHandler> _logger`

**Fluxo de Execução**:

1. **⭐ Log Inicial**: Registrar início do processamento
2. **🔍 Buscar Avaliação**: Obter avaliação pelo ID
3. **✅ Validações de Domínio**:
   - Avaliação existe
   - Avaliação não foi deletada
   - Avaliação não está aprovada
4. **👤 Aprovar Avaliação**: Chamar método `Approve(moderatorId)`
5. **💾 Persistir**: Atualizar no repositório e salvar mudanças
6. **📊 Log Final**: Registrar sucesso da operação
7. **📤 Retornar**: `ApiResponse<bool>.Ok(true, "Avaliação aprovada com sucesso.")`

### 3. ApproveProductReviewCommandValidator

**Interface**: `IValidator<ApproveProductReviewCommand>`  
**Namespace**: `CatalogService.Application.Commands.ProductReviews.ApproveProductReview`

**Validações**:
- `Id` não pode ser `Guid.Empty`
- `ModeratorId` não pode ser `Guid.Empty`

### 4. ApproveProductReviewResponse

**Namespace**: `CatalogService.Application.Commands.ProductReviews.ApproveProductReview`

```csharp
public class ApproveProductReviewResponse
{
    public Guid ReviewId { get; set; }
    public Guid ModeratorId { get; set; }
    public DateTime ApprovedAt { get; set; }
    public bool IsApproved { get; set; }
}
```

## 🔒 Regras de Negócio

### Validações de Domínio

1. **Avaliação Existente**: Verificar se a avaliação existe
2. **Avaliação Não Deletada**: Não é possível aprovar avaliação deletada
3. **Avaliação Não Aprovada**: Não é possível aprovar avaliação já aprovada
4. **ModeratorId Válido**: ID do moderador é obrigatório

### Regras de Aprovação

- A avaliação deve estar pendente (`IsApproved = false`)
- A data de moderação é registrada automaticamente
- O moderador que aprovou é registrado
- A versão da avaliação é incrementada
- A data de atualização é atualizada

## 📝 Logs Estruturados

### Log Inicial
```csharp
_logger.LogInformation("⭐ [ApproveProductReviewCommandHandler] Iniciando processamento para ReviewId: {ReviewId}, ModeratorId: {ModeratorId}", request.Id, request.ModeratorId);
```

### Log de Sucesso
```csharp
_logger.LogInformation("✅ [ApproveProductReviewCommandHandler] Avaliação {ReviewId} aprovada com sucesso pelo moderador {ModeratorId}", request.Id, request.ModeratorId);
```

## 🔄 Integração com Domain Model

### Método do Aggregate Root

O comando utilizará o método `Approve(Guid moderatorId)` já existente na classe `ProductReview`:

```csharp
public ProductReview Approve(Guid moderatorId)
{
    if (DeletedAt.HasValue)
        throw new DomainException("Não é possível aprovar uma avaliação deletada");

    if (IsApproved)
        throw new DomainException("Avaliação já foi aprovada");

    if (moderatorId == Guid.Empty)
        throw new ArgumentException("ID do moderador é obrigatório", nameof(moderatorId));

    IsApproved = true;
    ModeratedAt = DateTime.UtcNow;
    ModeratedBy = moderatorId;
    UpdatedAt = DateTime.UtcNow;
    Version++;

    return this;
}
```

## 🎯 Endpoint API

### Rota Proposta
```http
PATCH /api/products/{productId}/reviews/{reviewId}/approve
```

### Parâmetros
- `productId` (Guid): ID do produto (via route)
- `reviewId` (Guid): ID da avaliação (via route)
- `moderatorId` (Guid): ID do moderador (via body)

### Resposta
```json
{
  "success": true,
  "message": "Avaliação aprovada com sucesso.",
  "data": true,
  "errors": []
}
```

## 🧪 Testes Recomendados

### Testes Unitários
1. **Aprovação com Sucesso**: Verificar comportamento quando todos os dados são válidos
2. **Avaliação Não Encontrada**: Verificar lançamento de exceção quando avaliação não existe
3. **Avaliação Deletada**: Verificar lançamento de exceção quando avaliação foi deletada
4. **Avaliação Já Aprovada**: Verificar lançamento de exceção quando avaliação já está aprovada
5. **ModeratorId Inválido**: Verificar lançamento de exceção quando moderatorId é inválido

### Testes de Integração
1. **Persistência**: Verificar se as mudanças são salvas corretamente
2. **Transação**: Verificar rollback em caso de erro
3. **Logs**: Verificar se os logs são gerados corretamente

## 📋 Checklist de Implementação

- [ ] Criar `ApproveProductReviewCommand.cs`
- [ ] Criar `ApproveProductReviewCommandHandler.cs`
- [ ] Criar `ApproveProductReviewCommandValidator.cs`
- [ ] Criar `ApproveProductReviewResponse.cs`
- [ ] Adicionar endpoint no `ProductController`
- [ ] Configurar injeção de dependência (se necessário)
- [ ] Executar build e verificar erros
- [ ] Executar testes
- [ ] Verificar logs estruturados
- [ ] Documentar no `commands-queries.md`

## 🔍 Pontos de Atenção

1. **Transação**: O comando deve participar da transação gerenciada pelo `TransactionBehavior`
2. **Logs**: Seguir o padrão de logs com emojis e estrutura definida
3. **Validações**: Realizar todas as validações antes de modificar o estado
4. **Exceções**: Lançar exceções de domínio apropriadas para cada cenário
5. **Response**: Retornar `ApiResponse<bool>` com mensagem apropriada

## 📚 Referências

- Padrão seguido: `AddProductToFavoritesCommand`
- Domain Model: `ProductReview.Approve()` method
- Arquitetura: CQRS + Clean Architecture
- Logs: Padrão com emojis e estrutura definida no projeto