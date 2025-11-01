# Catalog Service - CQRS Commands & Queries

## Índice
- [Commands (Write Operations)](#commands-write-operations)
- [Queries (Read Operations)](#queries-read-operations)
- [Event Sourcing](#event-sourcing)

---

## Commands (Write Operations)

### 📦 Product Commands

#### 1. CreateProductCommand
```typescript
{
  name: string;
  slug: string;
  description: string;
  shortDescription: string;
  price: number;
  compareAtPrice?: number;
  costPrice?: number;
  categoryId: UUID;
  sku: string;
  barcode?: string;
  weight?: number;
  dimensions?: string;
  images: Array<{ url: string; altText?: string }>;
}
```
**Implementação:**
- Valida unicidade de `slug` e `sku`
- Insere registro na tabela `products`
- Insere imagens relacionadas em `product_images`
- Trigger `publish_product_created` dispara evento `ProductCreated` no outbox
- Retorna UUID do produto criado

---

#### 2. UpdateProductCommand
```typescript
{
  productId: UUID;
  name?: string;
  slug?: string;
  description?: string;
  price?: number;
  categoryId?: UUID;
  isActive?: boolean;
  isFeatured?: boolean;
  version: number; // Optimistic locking
}
```
**Implementação:**
- Verifica `version` para evitar conflitos (optimistic locking)
- Atualiza apenas campos fornecidos
- Trigger `update_products_version` incrementa versão automaticamente
- Trigger `publish_product_updated` detecta tipo de mudança (preço, estoque, etc.)
- Publica evento específico (`ProductPriceChanged`, `ProductStockChanged` ou `ProductUpdated`)

---

#### 3. UpdateProductPriceCommand
```typescript
{
  productId: UUID;
  newPrice: number;
  compareAtPrice?: number;
  reason?: string;
}
```
**Implementação:**
- Command especializado para mudanças de preço
- Registra preço anterior para auditoria
- Publica evento `ProductPriceChanged` com old/new price
- Pode integrar com serviço de pricing/promotions

---

#### 4. ReserveStockCommand
```typescript
{
  productId: UUID;
  quantity: number;
  orderId: UUID;
}
```
**Implementação:**
- Verifica disponibilidade (`stock - stock_reserved >= quantity`)
- Incrementa `stock_reserved`
- Publica evento `StockReserved` com detalhes da reserva
- Usado pelo Order Service durante criação de pedido
- Implementa idempotência usando `orderId` no inbox

---

#### 5. ReleaseStockCommand
```typescript
{
  productId: UUID;
  quantity: number;
  orderId: UUID;
  reason: 'CANCELLED' | 'EXPIRED' | 'REJECTED';
}
```
**Implementação:**
- Decrementa `stock_reserved`
- Publica evento `StockReleased`
- Chamado quando pedido é cancelado ou carrinho expira

---

#### 6. CommitStockCommand
```typescript
{
  productId: UUID;
  quantity: number;
  orderId: UUID;
}
```
**Implementação:**
- Decrementa ambos `stock` e `stock_reserved`
- Publica evento `StockCommitted`
- Executado quando pedido é confirmado/pago

---

#### 7. AdjustStockCommand
```typescript
{
  productId: UUID;
  adjustment: number; // Pode ser negativo
  reason: string;
  adjustedBy: UUID;
}
```
**Implementação:**
- Ajuste manual de estoque (recebimento, inventário, perdas)
- Registra no outbox para auditoria
- Atualiza `stock` diretamente

---

#### 8. SoftDeleteProductCommand
```typescript
{
  productId: UUID;
  deletedBy: UUID;
}
```
**Implementação:**
- Define `deleted_at = now()`
- Produto não aparece mais nas queries (índices com `WHERE deleted_at IS NULL`)
- Mantém histórico para referências de pedidos antigos
- Publica evento `ProductDeleted`

---

### 📁 Category Commands

#### 9. CreateCategoryCommand
```typescript
{
  name: string;
  slug: string;
  description?: string;
  parentId?: UUID;
  displayOrder?: number;
}
```
**Implementação:**
- Valida unicidade de `slug`
- Verifica existência de `parentId` se fornecido
- Insere registro em `categories`

---

#### 10. UpdateCategoryCommand
```typescript
{
  categoryId: UUID;
  name?: string;
  isActive?: boolean;
  displayOrder?: number;
}
```
**Implementação:**
- Atualização parcial de categoria
- Pode desativar categoria (afeta exibição de produtos)

---

### ⭐ Favorite Commands

#### 11. AddToFavoritesCommand
```typescript
{
  userId: UUID;
  productId: UUID;
}
```
**Implementação:**
- Insere em `favorite_products` (constraint UNIQUE evita duplicatas)
- Trigger `update_favorite_count_on_insert` incrementa contador no produto
- Operação idempotente (ON CONFLICT DO NOTHING)

---

#### 12. RemoveFromFavoritesCommand
```typescript
{
  userId: UUID;
  productId: UUID;
}
```
**Implementação:**
- Remove de `favorite_products`
- Trigger `update_favorite_count_on_delete` decrementa contador

---

### ⭐ Review Commands

#### 13. CreateReviewCommand
```typescript
{
  productId: UUID;
  userId: UUID;
  rating: number; // 1-5
  title: string;
  comment: string;
  isVerifiedPurchase: boolean;
}
```
**Implementação:**
- Valida rating entre 1-5
- Define `is_approved = false` (requer moderação)
- Trigger atualiza estatísticas do produto apenas quando aprovada
- Pode verificar compra através de evento `OrderCompleted` no inbox

---

#### 14. ApproveReviewCommand
```typescript
{
  reviewId: UUID;
  moderatorId: UUID;
}
```
**Implementação:**
- Define `is_approved = true`
- Registra `moderated_by` e `moderated_at`
- Trigger `update_review_stats_on_update` recalcula média e contador

---

#### 15. RejectReviewCommand
```typescript
{
  reviewId: UUID;
  moderatorId: UUID;
  reason: string;
}
```
**Implementação:**
- Soft delete da review (`deleted_at = now()`)
- Registra razão da rejeição no metadata

---

#### 16. VoteReviewCommand
```typescript
{
  reviewId: UUID;
  userId: UUID;
  isHelpful: boolean;
}
```
**Implementação:**
- Insere em `review_votes` (constraint UNIQUE evita duplicatas)
- Incrementa `helpful_count` ou `unhelpful_count` na review
- Idempotente usando UPSERT

---

### 🖼️ Image Commands

#### 17. AddProductImagesCommand
```typescript
{
  productId: UUID;
  images: Array<{
    url: string;
    thumbnailUrl?: string;
    altText?: string;
    displayOrder?: number;
    isPrimary?: boolean;
  }>;
}
```
**Implementação:**
- Insere múltiplas imagens em `product_images`
- Se `isPrimary = true`, desmarca outras imagens como primárias
- Atualiza `display_order` automaticamente

---

#### 18. SetPrimaryImageCommand
```typescript
{
  productId: UUID;
  imageId: UUID;
}
```
**Implementação:**
- Desmarca todas imagens do produto como primárias
- Marca a imagem especificada como `is_primary = true`

---

## Queries (Read Operations)

### 🔍 Product Queries

#### 1. GetProductByIdQuery
```typescript
{
  productId: UUID;
  includeInactive?: boolean;
}
```
**Implementação:**
- Usa view `product_catalog` para dados agregados
- Retorna produto com categoria, imagens e estatísticas
- Filtra `deleted_at IS NULL` por padrão

**Response:**
```typescript
{
  id: UUID;
  name: string;
  slug: string;
  description: string;
  price: number;
  stock: number;
  category: { id, name, slug };
  images: Array<{ url, thumbnailUrl, altText }>;
  reviewCount: number;
  reviewAvgRating: number;
  favoriteCount: number;
}
```

---

#### 2. GetProductBySlugQuery
```typescript
{
  slug: string;
}
```
**Implementação:**
- Similar ao `GetProductById` mas busca por slug
- Usa índice `idx_products_slug` para performance
- Incrementa `view_count` (pode ser async via evento)

---

#### 3. SearchProductsQuery
```typescript
{
  searchTerm?: string;
  categoryId?: UUID;
  minPrice?: number;
  maxPrice?: number;
  inStock?: boolean;
  isFeatured?: boolean;
  sortBy?: 'price' | 'name' | 'rating' | 'newest';
  sortOrder?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}
```
**Implementação:**
- Query complexa com múltiplos filtros opcionais
- Usa `pg_trgm` para full-text search em nome/descrição
- Aplica índices específicos para cada filtro
- Paginação usando LIMIT/OFFSET
- Retorna total de resultados para navegação

**Response:**
```typescript
{
  items: Product[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
```

---

#### 4. GetFeaturedProductsQuery
```typescript
{
  limit?: number;
  categoryId?: UUID;
}
```
**Implementação:**
- Usa índice `idx_products_featured`
- Ordenado por `created_at DESC` ou critério customizado
- Cache agressivo (TTL longo)

---

#### 5. GetProductsByCategoryQuery
```typescript
{
  categoryId: UUID;
  includeSubcategories?: boolean;
  page?: number;
  pageSize?: number;
}
```
**Implementação:**
- Se `includeSubcategories = true`, busca recursivamente categorias filhas
- Usa CTE (Common Table Expression) para hierarquia
- Ordena por `display_order` ou relevância

---

#### 6. GetLowStockProductsQuery
```typescript
{
  threshold?: number;
  page?: number;
  pageSize?: number;
}
```
**Implementação:**
- Filtra `stock <= low_stock_threshold`
- Usado por dashboard administrativo
- Pode gerar alertas/notificações

---

#### 7. GetProductStockQuery
```typescript
{
  productId: UUID;
}
```
**Implementação:**
- Retorna informações detalhadas de estoque
```typescript
{
  stock: number;
  stockReserved: number;
  available: number; // stock - stock_reserved
  lowStockThreshold: number;
  isLowStock: boolean;
}
```

---

### 📁 Category Queries

#### 8. GetCategoryByIdQuery
```typescript
{
  categoryId: UUID;
}
```
**Implementação:**
- Busca direta na tabela `categories`
- Inclui categoria pai se existir

---

#### 9. GetCategoryTreeQuery
```typescript
{
  rootCategoryId?: UUID;
  maxDepth?: number;
}
```
**Implementação:**
- Retorna árvore hierárquica de categorias
- Usa CTE recursiva para construir árvore
- Ordena por `display_order`

**Response:**
```typescript
{
  id: UUID;
  name: string;
  slug: string;
  children: CategoryTree[];
  productCount: number;
}
```

---

#### 10. GetActiveCategoriesQuery
```typescript
{
  parentId?: UUID;
}
```
**Implementação:**
- Filtra `is_active = true`
- Usado para menus de navegação
- Cache de longa duração

---

### ⭐ Favorite Queries

#### 11. GetUserFavoritesQuery
```typescript
{
  userId: UUID;
  page?: number;
  pageSize?: number;
}
```
**Implementação:**
- JOIN entre `favorite_products` e `product_catalog` view
- Ordenado por `created_at DESC`
- Retorna produtos completos com imagens

---

#### 12. CheckIsFavoriteQuery
```typescript
{
  userId: UUID;
  productId: UUID;
}
```
**Implementação:**
- Consulta simples em `favorite_products`
- Retorna boolean
- Usado para toggle de favorito no frontend

---

### ⭐ Review Queries

#### 13. GetProductReviewsQuery
```typescript
{
  productId: UUID;
  onlyApproved?: boolean;
  sortBy?: 'recent' | 'helpful' | 'rating';
  page?: number;
  pageSize?: number;
}
```
**Implementação:**
- Filtra `is_approved = true` por padrão
- JOIN com informações do usuário (nome pode vir de cache)
- Inclui contadores de votos

**Response:**
```typescript
{
  items: Array<{
    id: UUID;
    rating: number;
    title: string;
    comment: string;
    userName: string;
    isVerifiedPurchase: boolean;
    helpfulCount: number;
    createdAt: Date;
  }>;
  total: number;
}
```

---

#### 14. GetReviewsByUserQuery
```typescript
{
  userId: UUID;
  page?: number;
  pageSize?: number;
}
```
**Implementação:**
- Busca todas reviews de um usuário
- JOIN com produtos para exibir nome/imagem
- Útil para "minhas avaliações"

---

#### 15. GetPendingReviewsQuery
```typescript
{
  page?: number;
  pageSize?: number;
}
```
**Implementação:**
- Filtra `is_approved = false AND deleted_at IS NULL`
- Usado por moderadores
- Ordenado por `created_at ASC` (FIFO)

---

#### 16. GetReviewStatsQuery
```typescript
{
  productId: UUID;
}
```
**Implementação:**
- Agregação de estatísticas de reviews
```typescript
{
  totalReviews: number;
  averageRating: number;
  ratingDistribution: {
    1: number,
    2: number,
    3: number,
    4: number,
    5: number
  }
}
```
- Pode ser cacheado e atualizado via trigger

---

## Event Sourcing

### Outbox Events (Published)

**ProductCreated**
```json
{
  "eventType": "ProductCreated",
  "payload": {
    "productId": "uuid",
    "name": "string",
    "price": 99.90,
    "stock": 100
  }
}
```

**ProductPriceChanged**
```json
{
  "eventType": "ProductPriceChanged",
  "payload": {
    "productId": "uuid",
    "oldPrice": 99.90,
    "newPrice": 89.90
  }
}
```

**StockReserved**
```json
{
  "eventType": "StockReserved",
  "payload": {
    "productId": "uuid",
    "quantity": 2,
    "orderId": "uuid"
  }
}
```

**StockCommitted**
```json
{
  "eventType": "StockCommitted",
  "payload": {
    "productId": "uuid",
    "quantity": 2,
    "orderId": "uuid"
  }
}
```

---

### Inbox Events (Consumed)

**OrderCompleted**
```json
{
  "eventType": "OrderCompleted",
  "payload": {
    "orderId": "uuid",
    "userId": "uuid",
    "items": [{"productId": "uuid", "quantity": 1}]
  }
}
```
**Handler:** Marca `is_verified_purchase = true` para reviews deste usuário/produto

**OrderCancelled**
```json
{
  "eventType": "OrderCancelled",
  "payload": {
    "orderId": "uuid",
    "items": [{"productId": "uuid", "quantity": 1}]
  }
}
```
**Handler:** Executa `ReleaseStockCommand` para cada item

---

## Boas Práticas Implementadas

### ✅ Command Side
- **Idempotência:** Usar `inbox_events` para rastrear eventos processados
- **Optimistic Locking:** Campo `version` previne race conditions
- **Soft Deletes:** Manter histórico com `deleted_at`
- **Event Publishing:** Triggers automáticos para Outbox Pattern
- **Validações:** Constraints no banco + validações na aplicação

### ✅ Query Side
- **Read Models:** View `product_catalog` pré-agrega dados
- **Índices Estratégicos:** Cobrem todos os principais filtros
- **Caching:** Queries de catálogo podem ter TTL longo
- **Paginação:** Sempre implementar para evitar sobrecarga
- **Projeções:** Retornar apenas dados necessários

### ✅ Separação CQRS
- Commands modificam estado e publicam eventos
- Queries leem de views otimizadas
- Eventual consistency aceita (estatísticas podem estar levemente desatualizadas)
- Commands retornam apenas ID/status, não objetos completos

---

## Diagrama de Fluxo

```
┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│   Command    │────────▶│  Domain      │────────▶│   Outbox     │
│   Handler    │         │  Model       │         │   Events     │
└──────────────┘         └──────────────┘         └──────────────┘
                                │                         │
                                ▼                         ▼
                         ┌──────────────┐         ┌──────────────┐
                         │  Database    │         │   Message    │
                         │  (Write)     │         │   Broker     │
                         └──────────────┘         └──────────────┘
                                                          │
                                                          ▼
                         ┌──────────────┐         ┌──────────────┐
                         │    Query     │◀────────│   Event      │
                         │    Handler   │         │   Handler    │
                         └──────────────┘         └──────────────┘
                                │
                                ▼
                         ┌──────────────┐
                         │  Read Model  │
                         │  (View)      │
                         └──────────────┘
```

---

**Documentação Técnica - Catalog Service v1.0**  
*Arquitetura: CQRS + Event Sourcing + Outbox Pattern*