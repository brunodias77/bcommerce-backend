# 📋 Commands & Queries - Catalog Service

Este documento define todos os **Commands** (operações de escrita) e **Queries** (operações de leitura) que devemos implementar no serviço de Catálogo do projeto BCommerce.

## 📊 Status Geral

- ✅ **Implementado**
- 🚧 **Em Desenvolvimento**
- ⏳ **Pendente**
- 🔄 **Refatoração Necessária**

---

## 🔧 COMMANDS (Write Operations)

### 📂 Categories

#### ✅ CreateCategory

**Status**: Implementado  
**Descrição**: Cria uma nova categoria no catálogo  
**Parâmetros**:

- `Name` (string, obrigatório): Nome da categoria
- `Slug` (string, obrigatório): URL amigável única
- `Description` (string, opcional): Descrição da categoria
- `ParentId` (Guid?, opcional): ID da categoria pai
- `DisplayOrder` (int): Ordem de exibição (padrão: 0)
- `IsActive` (bool): Status ativo (padrão: true)
- `Metadata` (string): JSON com metadados (padrão: "{}")

**Resposta**: `ApiResponse<CreateCategoryResponse>`  
**Validações**:

- Nome obrigatório (máx. 200 caracteres)
- Slug único e válido (formato: a-z, 0-9, hífens)
- Descrição opcional (máx. 1000 caracteres)
- Metadata deve ser JSON válido

---

#### ✅ UpdateCategory

**Status**: Implementado  
**Descrição**: Atualiza informações de uma categoria existente  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID da categoria
- `Name` (string, obrigatório): Nome da categoria
- `Slug` (string, obrigatório): URL amigável única
- `Description` (string, opcional): Descrição da categoria
- `ParentId` (Guid?, opcional): ID da categoria pai
- `DisplayOrder` (int): Ordem de exibição
- `Metadata` (string): JSON com metadados

**Resposta**: `ApiResponse<UpdateCategoryResponse>`  
**Validações**:

- Categoria deve existir
- Slug único (exceto para a própria categoria)
- Não pode ser pai de si mesma
- Não pode criar ciclos na hierarquia

---

#### ✅ DeleteCategory

**Status**: Implementado  
**Descrição**: Remove uma categoria (soft delete)  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID da categoria

**Resposta**: `ApiResponse<bool>`  
**Validações**:

- Categoria deve existir
- Não pode ter produtos associados
- Não pode ter subcategorias ativas

---

#### ✅ ActivateCategory

**Status**: Implementado  
**Descrição**: Ativa uma categoria desativada  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID da categoria

**Resposta**: `ApiResponse<bool>`  
**Validações**:

- Categoria deve existir
- Categoria deve estar inativa

---

#### ⏳ DeactivateCategory

**Status**: Pendente  
**Descrição**: Desativa uma categoria ativa  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID da categoria

**Resposta**: `ApiResponse<bool>`  
**Validações**:

- Categoria deve existir
- Categoria deve estar ativa

---

### 🛍️ Products

#### ✅ CreateProduct

**Status**: Implementado  
**Descrição**: Cria um novo produto no catálogo  
**Parâmetros**:

- `Name` (string, obrigatório): Nome do produto
- `Slug` (string, obrigatório): URL amigável única
- `Description` (string, opcional): Descrição completa
- `ShortDescription` (string, opcional): Descrição resumida
- `Price` (decimal, obrigatório): Preço do produto
- `Currency` (string): Moeda (padrão: "BRL")
- `CompareAtPrice` (decimal?, opcional): Preço de comparação
- `CostPrice` (decimal?, opcional): Preço de custo
- `Stock` (int): Quantidade em estoque
- `LowStockThreshold` (int): Limite de estoque baixo
- `CategoryId` (Guid?, opcional): ID da categoria
- `MetaTitle` (string, opcional): Título SEO
- `MetaDescription` (string, opcional): Descrição SEO
- `WeightKg` (decimal?, opcional): Peso em kg
- `Sku` (string, opcional): Código SKU
- `Barcode` (string, opcional): Código de barras
- `IsActive` (bool): Status ativo
- `IsFeatured` (bool): Produto em destaque

**Resposta**: `ApiResponse<CreateProductResponse>`  
**Validações**:

- Nome obrigatório (máx. 200 caracteres)
- Slug único e válido
- Preço maior que zero
- Estoque não negativo

---

#### ✅ UpdateProduct

**Status**: Implementado  
**Descrição**: Atualiza informações de um produto existente  
**Parâmetros**: Similares ao CreateProduct + `Id`  
**Resposta**: `ApiResponse<UpdateProductResponse>`  
**Validações**: Similares ao CreateProduct + produto deve existir
**Endpoint**: `PUT /api/products/{id}`

---

#### ✅ DeleteProduct

**Status**: Implementado  
**Descrição**: Remove um produto (soft delete)  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID do produto

**Resposta**: `ApiResponse<bool>`  
**Validações**:

- Produto deve existir
- Não pode ter pedidos pendentes

---

#### ✅ ActivateProduct

**Status**: Implementado  
**Descrição**: Ativa um produto desativado  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID do produto

**Resposta**: `ApiResponse<bool>`

---

#### ✅ DeactivateProduct

**Status**: Implementado  
**Descrição**: Desativa um produto ativo  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID do produto

**Resposta**: `ApiResponse<bool>`

---

#### ✅ UpdateProductStock

**Status**: Implementado  
**Descrição**: Atualiza o estoque de um produto  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID do produto
- `Stock` (int, obrigatório): Nova quantidade
- `Operation` (enum): ADD, SUBTRACT, SET

**Resposta**: `ApiResponse<ProductStockResponse>`

---

#### ✅ UpdateProductPrice

**Status**: Implementado  
**Descrição**: Atualiza o preço de um produto  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID do produto
- `Price` (decimal, obrigatório): Novo preço
- `CompareAtPrice` (decimal?, opcional): Preço de comparação

**Resposta**: `ApiResponse<ProductPriceResponse>`

---

#### ✅ FeatureProduct

**Status**: Implementado  
**Descrição**: Marca um produto como destaque  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID do produto

**Resposta**: `ApiResponse<bool>`

---

#### ✅ UnfeatureProduct

**Status**: Implementado  
**Descrição**: Remove um produto dos destaques  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID do produto

**Resposta**: `ApiResponse<bool>`

---

### 🖼️ Product Images

#### ✅ AddProductImage

**Status**: ✅ Implementado  
**Descrição**: Adiciona uma imagem a um produto  
**Parâmetros**:

- `ProductId` (Guid, obrigatório): ID do produto
- `Url` (string, obrigatório): URL da imagem
- `ThumbnailUrl` (string, opcional): URL da miniatura
- `AltText` (string, opcional): Texto alternativo
- `DisplayOrder` (int): Ordem de exibição
- `IsPrimary` (bool): Imagem principal

**Resposta**: `ApiResponse<ProductImageResponse>`

---

#### ✅ UpdateProductImage

**Status**: ✅ Implementado  
**Descrição**: Atualiza informações de uma imagem  
**Parâmetros**: Similares ao AddProductImage + `Id`  
**Resposta**: `ApiResponse<UpdateProductImageResponse>`

---

#### ⏳ DeleteProductImage

**Status**: ✅ Implementado  
**Descrição**: Remove uma imagem do produto  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID da imagem

**Resposta**: `ApiResponse<bool>`

---

#### ⏳ SetPrimaryProductImage

**Status**: ✅ Implementado    
**Descrição**: Define uma imagem como principal  
**Parâmetros**:

- `ProductId` (Guid, obrigatório): ID do produto
- `ImageId` (Guid, obrigatório): ID da imagem

**Resposta**: `ApiResponse<bool>`

---

#### ⏳ ReorderProductImages

**Status**: ✅ Implementado   
**Descrição**: Reordena as imagens de um produto  
**Parâmetros**:

- `ProductId` (Guid, obrigatório): ID do produto
- `ImageOrders` (List<ImageOrder>): Lista com ID e nova ordem

**Resposta**: `ApiResponse<bool>`

---

### ⭐ Product Reviews

#### ⏳ CreateProductReview

**Status**:  ✅ Implementado   
**Descrição**: Cria uma avaliação para um produto  
**Parâmetros**:

- `ProductId` (Guid, obrigatório): ID do produto
- `UserId` (Guid, obrigatório): ID do usuário
- `Rating` (int, obrigatório): Nota de 1 a 5
- `Title` (string, opcional): Título da avaliação
- `Comment` (string, opcional): Comentário
- `IsVerifiedPurchase` (bool): Compra verificada

**Resposta**: `ApiResponse<ProductReviewResponse>`

---

#### ⏳ UpdateProductReview

**Status**: ✅ Implementado  
**Descrição**: Atualiza uma avaliação existente  
**Parâmetros**: Similares ao CreateProductReview + `Id`  
**Resposta**: `ApiResponse<ProductReviewResponse>`

---

#### ⏳ DeleteProductReview

**Status**: ✅ Implementado    
**Descrição**: Remove uma avaliação  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID da avaliação

**Resposta**: `ApiResponse<bool>`

---

#### ⏳ ApproveProductReview

**Status**: Pendente  
**Descrição**: Aprova uma avaliação para publicação  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID da avaliação
- `ModeratorId` (Guid, obrigatório): ID do moderador

**Resposta**: `ApiResponse<bool>`

---

#### ⏳ RejectProductReview

**Status**: Pendente  
**Descrição**: Rejeita uma avaliação  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID da avaliação
- `ModeratorId` (Guid, obrigatório): ID do moderador
- `Reason` (string, opcional): Motivo da rejeição

**Resposta**: `ApiResponse<bool>`

---

#### ⏳ FeatureProductReview

**Status**: Pendente  
**Descrição**: Marca uma avaliação como destaque  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID da avaliação

**Resposta**: `ApiResponse<bool>`

---

#### ⏳ UnfeatureProductReview

**Status**: Pendente  
**Descrição**: Remove uma avaliação dos destaques  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID da avaliação

**Resposta**: `ApiResponse<bool>`

---

### ❤️ Favorite Products

#### ⏳ AddProductToFavorites

**Status**: Pendente  
**Descrição**: Adiciona um produto aos favoritos do usuário  
**Parâmetros**:

- `UserId` (Guid, obrigatório): ID do usuário
- `ProductId` (Guid, obrigatório): ID do produto

**Resposta**: `ApiResponse<bool>`

---

#### ⏳ RemoveProductFromFavorites

**Status**: Pendente  
**Descrição**: Remove um produto dos favoritos  
**Parâmetros**:

- `UserId` (Guid, obrigatório): ID do usuário
- `ProductId` (Guid, obrigatório): ID do produto

**Resposta**: `ApiResponse<bool>`

---

### 👍 Review Votes

#### ⏳ VoteReviewHelpful

**Status**: Pendente  
**Descrição**: Marca uma avaliação como útil  
**Parâmetros**:

- `ReviewId` (Guid, obrigatório): ID da avaliação
- `UserId` (Guid, obrigatório): ID do usuário

**Resposta**: `ApiResponse<bool>`

---

#### ⏳ VoteReviewUnhelpful

**Status**: Pendente  
**Descrição**: Marca uma avaliação como não útil  
**Parâmetros**:

- `ReviewId` (Guid, obrigatório): ID da avaliação
- `UserId` (Guid, obrigatório): ID do usuário

**Resposta**: `ApiResponse<bool>`

---

#### ⏳ RemoveReviewVote

**Status**: Pendente  
**Descrição**: Remove um voto de uma avaliação  
**Parâmetros**:

- `ReviewId` (Guid, obrigatório): ID da avaliação
- `UserId` (Guid, obrigatório): ID do usuário

**Resposta**: `ApiResponse<bool>`

---

## 🔍 QUERIES (Read Operations)

### 📂 Categories

#### ⏳ GetCategoryById

**Status**: Pendente  
**Descrição**: Busca uma categoria por ID  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID da categoria

**Resposta**: `ApiResponse<CategoryResponse>`

---

#### ⏳ GetCategoriesByParent

**Status**: Pendente  
**Descrição**: Busca subcategorias de uma categoria pai  
**Parâmetros**:

- `ParentId` (Guid?, opcional): ID da categoria pai (null = raiz)
- `IncludeInactive` (bool): Incluir inativas (padrão: false)

**Resposta**: `ApiResponse<List<CategoryResponse>>`

---

#### ⏳ GetAllCategories

**Status**: Pendente  
**Descrição**: Lista todas as categorias com paginação  
**Parâmetros**:

- `Page` (int): Página (padrão: 1)
- `PageSize` (int): Itens por página (padrão: 20)
- `IncludeInactive` (bool): Incluir inativas

**Resposta**: `ApiResponse<PagedResult<CategoryResponse>>`

---

#### ⏳ GetActiveCategoriesTree

**Status**: Pendente  
**Descrição**: Retorna árvore hierárquica de categorias ativas  
**Parâmetros**: Nenhum  
**Resposta**: `ApiResponse<List<CategoryTreeResponse>>`

---

#### ⏳ SearchCategories

**Status**: Pendente  
**Descrição**: Busca categorias por termo  
**Parâmetros**:

- `SearchTerm` (string, obrigatório): Termo de busca
- `Page` (int): Página
- `PageSize` (int): Itens por página

**Resposta**: `ApiResponse<PagedResult<CategoryResponse>>`

---

### 🛍️ Products

#### ⏳ GetProductById

**Status**: Pendente  
**Descrição**: Busca um produto por ID  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID do produto
- `IncludeImages` (bool): Incluir imagens
- `IncludeReviews` (bool): Incluir avaliações

**Resposta**: `ApiResponse<ProductDetailResponse>`

---

#### ⏳ GetProductBySlug

**Status**: Pendente  
**Descrição**: Busca um produto por slug  
**Parâmetros**:

- `Slug` (string, obrigatório): Slug do produto
- `IncludeImages` (bool): Incluir imagens
- `IncludeReviews` (bool): Incluir avaliações

**Resposta**: `ApiResponse<ProductDetailResponse>`

---

#### ⏳ GetProductsByCategory

**Status**: Pendente  
**Descrição**: Lista produtos de uma categoria  
**Parâmetros**:

- `CategoryId` (Guid, obrigatório): ID da categoria
- `Page` (int): Página
- `PageSize` (int): Itens por página
- `SortBy` (enum): Ordenação (Name, Price, CreatedAt, Rating)
- `SortDirection` (enum): ASC, DESC

**Resposta**: `ApiResponse<PagedResult<ProductSummaryResponse>>`

---

#### ⏳ GetFeaturedProducts

**Status**: Pendente  
**Descrição**: Lista produtos em destaque  
**Parâmetros**:

- `Limit` (int): Quantidade máxima (padrão: 10)

**Resposta**: `ApiResponse<List<ProductSummaryResponse>>`

---

#### ⏳ GetActiveProducts

**Status**: Pendente  
**Descrição**: Lista produtos ativos com paginação  
**Parâmetros**:

- `Page` (int): Página
- `PageSize` (int): Itens por página
- `SortBy` (enum): Ordenação
- `SortDirection` (enum): Direção

**Resposta**: `ApiResponse<PagedResult<ProductSummaryResponse>>`

---

#### ⏳ SearchProducts

**Status**: Pendente  
**Descrição**: Busca produtos por termo  
**Parâmetros**:

- `SearchTerm` (string, obrigatório): Termo de busca
- `CategoryId` (Guid?, opcional): Filtrar por categoria
- `MinPrice` (decimal?, opcional): Preço mínimo
- `MaxPrice` (decimal?, opcional): Preço máximo
- `Page` (int): Página
- `PageSize` (int): Itens por página

**Resposta**: `ApiResponse<PagedResult<ProductSummaryResponse>>`

---

#### ⏳ GetProductsWithLowStock

**Status**: Pendente  
**Descrição**: Lista produtos com estoque baixo  
**Parâmetros**:

- `Page` (int): Página
- `PageSize` (int): Itens por página

**Resposta**: `ApiResponse<PagedResult<ProductStockResponse>>`

---

#### ⏳ GetProductsByPriceRange

**Status**: Pendente  
**Descrição**: Lista produtos por faixa de preço  
**Parâmetros**:

- `MinPrice` (decimal, obrigatório): Preço mínimo
- `MaxPrice` (decimal, obrigatório): Preço máximo
- `Page` (int): Página
- `PageSize` (int): Itens por página

**Resposta**: `ApiResponse<PagedResult<ProductSummaryResponse>>`

---

### 🖼️ Product Images

#### ⏳ GetProductImages

**Status**: Pendente  
**Descrição**: Lista imagens de um produto  
**Parâmetros**:

- `ProductId` (Guid, obrigatório): ID do produto

**Resposta**: `ApiResponse<List<ProductImageResponse>>`

---

#### ⏳ GetPrimaryProductImage

**Status**: Pendente  
**Descrição**: Busca a imagem principal de um produto  
**Parâmetros**:

- `ProductId` (Guid, obrigatório): ID do produto

**Resposta**: `ApiResponse<ProductImageResponse>`

---

### ⭐ Product Reviews

#### ⏳ GetProductReviews

**Status**: Pendente  
**Descrição**: Lista avaliações de um produto  
**Parâmetros**:

- `ProductId` (Guid, obrigatório): ID do produto
- `Page` (int): Página
- `PageSize` (int): Itens por página
- `OnlyApproved` (bool): Apenas aprovadas (padrão: true)

**Resposta**: `ApiResponse<PagedResult<ProductReviewResponse>>`

---

#### ⏳ GetReviewById

**Status**: Pendente  
**Descrição**: Busca uma avaliação por ID  
**Parâmetros**:

- `Id` (Guid, obrigatório): ID da avaliação

**Resposta**: `ApiResponse<ProductReviewDetailResponse>`

---

#### ⏳ GetReviewsByUser

**Status**: Pendente  
**Descrição**: Lista avaliações de um usuário  
**Parâmetros**:

- `UserId` (Guid, obrigatório): ID do usuário
- `Page` (int): Página
- `PageSize` (int): Itens por página

**Resposta**: `ApiResponse<PagedResult<ProductReviewResponse>>`

---

#### ⏳ GetFeaturedReviews

**Status**: Pendente  
**Descrição**: Lista avaliações em destaque  
**Parâmetros**:

- `Limit` (int): Quantidade máxima (padrão: 5)

**Resposta**: `ApiResponse<List<ProductReviewResponse>>`

---

#### ⏳ GetPendingReviews

**Status**: Pendente  
**Descrição**: Lista avaliações pendentes de moderação  
**Parâmetros**:

- `Page` (int): Página
- `PageSize` (int): Itens por página

**Resposta**: `ApiResponse<PagedResult<ProductReviewResponse>>`

---

### ❤️ Favorite Products

#### ⏳ GetUserFavoriteProducts

**Status**: Pendente  
**Descrição**: Lista produtos favoritos de um usuário  
**Parâmetros**:

- `UserId` (Guid, obrigatório): ID do usuário
- `Page` (int): Página
- `PageSize` (int): Itens por página

**Resposta**: `ApiResponse<PagedResult<ProductSummaryResponse>>`

---

#### ⏳ CheckIfProductIsFavorited

**Status**: Pendente  
**Descrição**: Verifica se um produto está nos favoritos  
**Parâmetros**:

- `UserId` (Guid, obrigatório): ID do usuário
- `ProductId` (Guid, obrigatório): ID do produto

**Resposta**: `ApiResponse<bool>`

---

### 📊 Statistics/Analytics

#### ⏳ GetProductViewCount

**Status**: Pendente  
**Descrição**: Retorna contagem de visualizações de um produto  
**Parâmetros**:

- `ProductId` (Guid, obrigatório): ID do produto

**Resposta**: `ApiResponse<int>`

---

#### ⏳ GetProductFavoriteCount

**Status**: Pendente  
**Descrição**: Retorna contagem de favoritos de um produto  
**Parâmetros**:

- `ProductId` (Guid, obrigatório): ID do produto

**Resposta**: `ApiResponse<int>`

---

#### ⏳ GetProductReviewStats

**Status**: Pendente  
**Descrição**: Retorna estatísticas de avaliações de um produto  
**Parâmetros**:

- `ProductId` (Guid, obrigatório): ID do produto

**Resposta**: `ApiResponse<ProductReviewStatsResponse>`

---

#### ⏳ GetCategoryProductCount

**Status**: Pendente  
**Descrição**: Retorna contagem de produtos por categoria  
**Parâmetros**:

- `CategoryId` (Guid, obrigatório): ID da categoria
- `IncludeSubcategories` (bool): Incluir subcategorias

**Resposta**: `ApiResponse<int>`

---

## 📝 Notas de Implementação

### 🏗️ Estrutura de Pastas Sugerida

```
CatalogService.Application/
├── Commands/
│   ├── Categories/
│   │   ├── CreateCategory/ ✅
│   │   ├── UpdateCategory/
│   │   ├── DeleteCategory/
│   │   ├── ActivateCategory/
│   │   └── DeactivateCategory/
│   ├── Products/
│   │   ├── CreateProduct/ ✅
│   │   ├── UpdateProduct/
│   │   ├── DeleteProduct/
│   │   ├── UpdateProductStock/
│   │   └── UpdateProductPrice/
│   ├── ProductImages/
│   ├── ProductReviews/
│   ├── FavoriteProducts/
│   └── ReviewVotes/
└── Queries/
    ├── Categories/
    ├── Products/
    ├── ProductImages/
    ├── ProductReviews/
    ├── FavoriteProducts/
    └── Statistics/
```

### 🔧 Padrões de Implementação

1. **Commands**: Usar padrão CQRS com handlers separados
2. **Validação**: FluentValidation para commands, validação de domínio para aggregates
3. **Transações**: UnitOfWork para operações que modificam dados
4. **Responses**: ApiResponse wrapper para todas as respostas
5. **Paginação**: PagedResult para listas grandes
6. **Logging**: Structured logging com contexto

### 🚀 Prioridades de Implementação

**Fase 1 - Core**:

- ✅ CreateCategory
- ✅ CreateProduct
- GetProductById
- GetProductBySlug
- GetCategoryById

**Fase 2 - CRUD Completo**:

- UpdateProduct
- UpdateCategory
- DeleteProduct
- DeleteCategory

**Fase 3 - Features Avançadas**:

- SearchProducts
- ProductImages
- ProductReviews
- FavoriteProducts

**Fase 4 - Analytics**:

- Statistics queries
- Review votes
- Advanced filtering

---

_Documento atualizado em: 01/11/2024_  
_Versão: 1.0_
