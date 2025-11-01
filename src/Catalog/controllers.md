# Catalog Service - Controllers Organization (CQRS)

## Índice

- [ProductController](#productcontroller)
- [CategoryController](#categorycontroller)
- [ProductReviewController](#productreviewcontroller)
- [FavoriteController](#favoritecontroller)
- [ProductImageController](#productimagecontroller)
- [AdminProductController](#adminproductcontroller)
- [AdminReviewController](#adminreviewcontroller)

---

## ProductController

**Base Path:** `/api/products`  
**Descrição:** Operações públicas de consulta e gerenciamento básico de produtos

### Queries (Read)

#### `GET /api/products/:id`

**Query:** `GetProductByIdQuery`

```typescript
Response: {
  id: UUID;
  name: string;
  slug: string;
  description: string;
  shortDescription: string;
  price: number;
  compareAtPrice?: number;
  stock: number;
  category: { id, name, slug };
  images: Array<{ url, thumbnailUrl, altText }>;
  reviewCount: number;
  reviewAvgRating: number;
  favoriteCount: number;
  isActive: boolean;
  isFeatured: boolean;
}
```

**Implementação:**

- Usa view `product_catalog`
- Incrementa `view_count` (async)
- Cache: 5 minutos

---

#### `GET /api/products/slug/:slug`

**Query:** `GetProductBySlugQuery`

```typescript
Response: Product(same as above);
```

**Implementação:**

- Busca por slug usando índice otimizado
- Usado para URLs amigáveis (SEO)
- Cache: 5 minutos

---

#### `GET /api/products/search`

**Query:** `SearchProductsQuery`

```typescript
Query Params: {
  q?: string;              // Search term
  categoryId?: UUID;
  minPrice?: number;
  maxPrice?: number;
  inStock?: boolean;
  isFeatured?: boolean;
  sortBy?: 'price' | 'name' | 'rating' | 'newest';
  sortOrder?: 'asc' | 'desc';
  page?: number;           // Default: 1
  pageSize?: number;       // Default: 20, Max: 100
}

Response: {
  items: Product[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
  filters: {
    categories: Array<{ id, name, count }>;
    priceRange: { min, max };
  }
}
```

**Implementação:**

- Full-text search com `pg_trgm`
- Múltiplos filtros combinados
- Faceted search para filtros dinâmicos
- Cache por combinação de filtros: 2 minutos

---

#### `GET /api/products/featured`

**Query:** `GetFeaturedProductsQuery`

```typescript
Query Params: {
  limit?: number;      // Default: 12
  categoryId?: UUID;
}

Response: {
  items: Product[];
}
```

**Implementação:**

- Produtos marcados como `is_featured = true`
- Usado em homepage/banners
- Cache agressivo: 15 minutos

---

#### `GET /api/products/:id/stock`

**Query:** `GetProductStockQuery`

```typescript
Response: {
  productId: UUID;
  stock: number;
  stockReserved: number;
  available: number; // stock - stock_reserved
  lowStockThreshold: number;
  isLowStock: boolean;
  isInStock: boolean;
}
```

**Implementação:**

- Consulta em tempo real (sem cache)
- Usado para validação de carrinho
- Rate limit: 100 req/min por IP

---

### Commands (Write)

#### `POST /api/products`

**Command:** `CreateProductCommand`  
**Auth:** Required (Admin/Seller)

```typescript
Request Body: {
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
  stock: number;
  lowStockThreshold?: number;
  images: Array<{
    url: string;
    thumbnailUrl?: string;
    altText?: string;
    isPrimary?: boolean;
  }>;
  metaTitle?: string;
  metaDescription?: string;
}

Response: {
  productId: UUID;
  slug: string;
}
```

**Implementação:**

- Valida unicidade de slug/SKU
- Transação: produto + imagens
- Publica `ProductCreated` event
- Invalida cache de categorias

---

#### `PUT /api/products/:id`

**Command:** `UpdateProductCommand`  
**Auth:** Required (Admin/Seller)

```typescript
Request Body: {
  name?: string;
  slug?: string;
  description?: string;
  shortDescription?: string;
  price?: number;
  compareAtPrice?: number;
  categoryId?: UUID;
  stock?: number;
  isActive?: boolean;
  isFeatured?: boolean;
  weight?: number;
  dimensions?: string;
  metaTitle?: string;
  metaDescription?: string;
  version: number;         // Optimistic locking
}

Response: {
  productId: UUID;
  version: number;
}
```

**Implementação:**

- Optimistic locking com version
- Atualização parcial
- Publica eventos específicos (PriceChanged, StockChanged)
- Invalida cache do produto

---

#### `PATCH /api/products/:id/price`

**Command:** `UpdateProductPriceCommand`  
**Auth:** Required (Admin/Pricing Manager)

```typescript
Request Body: {
  newPrice: number;
  compareAtPrice?: number;
  reason?: string;
}

Response: {
  productId: UUID;
  oldPrice: number;
  newPrice: number;
}
```

**Implementação:**

- Command especializado para preços
- Registra histórico de mudanças
- Publica `ProductPriceChanged`
- Notifica serviços de pricing/promotion

---

#### `DELETE /api/products/:id`

**Command:** `SoftDeleteProductCommand`  
**Auth:** Required (Admin)

```typescript
Response: {
  productId: UUID;
  deletedAt: Date;
}
```

**Implementação:**

- Soft delete (`deleted_at = now()`)
- Valida se produto não tem pedidos pendentes
- Publica `ProductDeleted`
- Remove de índices de busca

---

## CategoryController

**Base Path:** `/api/categories`  
**Descrição:** Gerenciamento de categorias hierárquicas

### Queries (Read)

#### `GET /api/categories`

**Query:** `GetActiveCategoriesQuery`

```typescript
Query Params: {
  parentId?: UUID;    // null = root categories
}

Response: {
  items: Array<{
    id: UUID;
    name: string;
    slug: string;
    description?: string;
    parentId?: UUID;
    productCount: number;
    displayOrder: number;
  }>;
}
```

**Implementação:**

- Apenas categorias ativas
- Ordenado por `display_order`
- Cache: 30 minutos

---

#### `GET /api/categories/tree`

**Query:** `GetCategoryTreeQuery`

```typescript
Query Params: {
  rootCategoryId?: UUID;
  maxDepth?: number;
}

Response: {
  items: Array<{
    id: UUID;
    name: string;
    slug: string;
    children: CategoryTree[];
    productCount: number;
    level: number;
  }>;
}
```

**Implementação:**

- CTE recursiva para hierarquia
- Usado em menus de navegação
- Cache: 30 minutos

---

#### `GET /api/categories/:id`

**Query:** `GetCategoryByIdQuery`

```typescript
Response: {
  id: UUID;
  name: string;
  slug: string;
  description?: string;
  parent?: { id, name, slug };
  subcategories: Array<{ id, name, slug }>;
  productCount: number;
}
```

**Implementação:**

- Detalhes completos da categoria
- Inclui subcategorias
- Cache: 15 minutos

---

#### `GET /api/categories/:id/products`

**Query:** `GetProductsByCategoryQuery`

```typescript
Query Params: {
  includeSubcategories?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
}

Response: {
  category: { id, name, slug };
  items: Product[];
  total: number;
  page: number;
  pageSize: number;
}
```

**Implementação:**

- Lista produtos de uma categoria
- Opção de incluir subcategorias recursivamente
- Cache: 5 minutos

---

### Commands (Write)

#### `POST /api/categories`

**Command:** `CreateCategoryCommand`  
**Auth:** Required (Admin)

```typescript
Request Body: {
  name: string;
  slug: string;
  description?: string;
  parentId?: UUID;
  displayOrder?: number;
  metadata?: object;
}

Response: {
  categoryId: UUID;
}
```

**Implementação:**

- Valida slug único
- Verifica parentId existe
- Invalida cache de árvore

---

#### `PUT /api/categories/:id`

**Command:** `UpdateCategoryCommand`  
**Auth:** Required (Admin)

```typescript
Request Body: {
  name?: string;
  description?: string;
  isActive?: boolean;
  displayOrder?: number;
  metadata?: object;
}

Response: {
  categoryId: UUID;
}
```

**Implementação:**

- Atualização parcial
- Se `isActive = false`, produtos não aparecem em busca
- Invalida caches relacionados

---

## ProductReviewController

**Base Path:** `/api/products/:productId/reviews`  
**Descrição:** Reviews e avaliações de produtos

### Queries (Read)

#### `GET /api/products/:productId/reviews`

**Query:** `GetProductReviewsQuery`

```typescript
Query Params: {
  sortBy?: 'recent' | 'helpful' | 'rating_high' | 'rating_low';
  page?: number;
  pageSize?: number;
}

Response: {
  items: Array<{
    id: UUID;
    rating: number;
    title: string;
    comment: string;
    userName: string;
    userAvatar?: string;
    isVerifiedPurchase: boolean;
    helpfulCount: number;
    unhelpfulCount: number;
    createdAt: Date;
    hasVoted?: boolean;      // Se usuário logado já votou
    userVote?: boolean;       // true = helpful, false = unhelpful
  }>;
  total: number;
  page: number;
}
```

**Implementação:**

- Apenas reviews aprovadas
- JOIN com cache de usuários
- Marca votos do usuário atual
- Cache: 5 minutos

---

#### `GET /api/products/:productId/reviews/stats`

**Query:** `GetReviewStatsQuery`

```typescript
Response: {
  totalReviews: number;
  averageRating: number;
  ratingDistribution: {
    5: number,
    4: number,
    3: number,
    2: number,
    1: number
  };
  verifiedPurchasePercentage: number;
}
```

**Implementação:**

- Agregações SQL
- Usado para exibir resumo
- Cache: 10 minutos

---

#### `GET /api/users/me/reviews`

**Query:** `GetReviewsByUserQuery`  
**Auth:** Required

```typescript
Query Params: {
  page?: number;
  pageSize?: number;
}

Response: {
  items: Array<{
    id: UUID;
    product: { id, name, slug, image };
    rating: number;
    title: string;
    comment: string;
    isApproved: boolean;
    createdAt: Date;
  }>;
  total: number;
}
```

**Implementação:**

- Reviews do usuário logado
- Inclui pendentes de aprovação
- Sem cache (dados privados)

---

### Commands (Write)

#### `POST /api/products/:productId/reviews`

**Command:** `CreateReviewCommand`  
**Auth:** Required

```typescript
Request Body: {
  rating: number;        // 1-5
  title: string;
  comment: string;
}

Response: {
  reviewId: UUID;
  status: 'pending_approval' | 'approved';
}
```

**Implementação:**

- Valida se usuário já não avaliou
- Verifica compra prévia (is_verified_purchase)
- Define `is_approved = false` (moderação)
- Publica `ReviewCreated` event
- Invalida cache de stats

---

#### `PUT /api/reviews/:id`

**Command:** `UpdateReviewCommand`  
**Auth:** Required (Owner only)

```typescript
Request Body: {
  rating?: number;
  title?: string;
  comment?: string;
}

Response: {
  reviewId: UUID;
}
```

**Implementação:**

- Apenas autor pode editar
- Reseta aprovação se editar após aprovação
- Invalida caches

---

#### `DELETE /api/reviews/:id`

**Command:** `DeleteReviewCommand`  
**Auth:** Required (Owner only)

```typescript
Response: {
  reviewId: UUID;
}
```

**Implementação:**

- Soft delete
- Atualiza estatísticas do produto
- Invalida caches

---

#### `POST /api/reviews/:id/vote`

**Command:** `VoteReviewCommand`  
**Auth:** Required

```typescript
Request Body: {
  isHelpful: boolean;
}

Response: {
  helpfulCount: number;
  unhelpfulCount: number;
}
```

**Implementação:**

- Idempotente (UPSERT)
- Um voto por usuário
- Atualiza contadores na review
- Sem invalidação de cache (eventual consistency)

---

## FavoriteController

**Base Path:** `/api/favorites`  
**Descrição:** Lista de favoritos/wishlist do usuário

### Queries (Read)

#### `GET /api/favorites`

**Query:** `GetUserFavoritesQuery`  
**Auth:** Required

```typescript
Query Params: {
  page?: number;
  pageSize?: number;
}

Response: {
  items: Array<{
    favoriteId: UUID;
    product: Product;
    addedAt: Date;
  }>;
  total: number;
}
```

**Implementação:**

- Favoritos do usuário logado
- JOIN com produtos completos
- Sem cache (dados privados)

---

#### `GET /api/favorites/check/:productId`

**Query:** `CheckIsFavoriteQuery`  
**Auth:** Required

```typescript
Response: {
  isFavorite: boolean;
}
```

**Implementação:**

- Verifica se produto está nos favoritos
- Usado para toggle no frontend
- Cache local: 1 minuto

---

### Commands (Write)

#### `POST /api/favorites`

**Command:** `AddToFavoritesCommand`  
**Auth:** Required

```typescript
Request Body: {
  productId: UUID;
}

Response: {
  favoriteId: UUID;
}
```

**Implementação:**

- Idempotente (ON CONFLICT DO NOTHING)
- Trigger incrementa `favorite_count`
- Retorna 200 mesmo se já existe

---

#### `DELETE /api/favorites/:productId`

**Command:** `RemoveFromFavoritesCommand`  
**Auth:** Required

```typescript
Response: {
  success: boolean;
}
```

**Implementação:**

- Remove do favoritos
- Trigger decrementa contador
- Retorna 200 mesmo se não existe

---

## ProductImageController

**Base Path:** `/api/products/:productId/images`  
**Descrição:** Gerenciamento de imagens de produtos

### Queries (Read)

#### `GET /api/products/:productId/images`

**Query:** `GetProductImagesQuery`

```typescript
Response: {
  items: Array<{
    id: UUID;
    url: string;
    thumbnailUrl: string;
    altText?: string;
    isPrimary: boolean;
    displayOrder: number;
  }>;
}
```

**Implementação:**

- Ordenado por `display_order`
- Cache: 10 minutos

---

### Commands (Write)

#### `POST /api/products/:productId/images`

**Command:** `AddProductImagesCommand`  
**Auth:** Required (Admin/Seller)

```typescript
Request Body: {
  images: Array<{
    url: string;
    thumbnailUrl?: string;
    altText?: string;
    isPrimary?: boolean;
  }>;
}

Response: {
  imageIds: UUID[];
}
```

**Implementação:**

- Upload múltiplo
- Integra com serviço de storage (S3)
- Gera thumbnails automaticamente
- Invalida cache do produto

---

#### `PUT /api/products/:productId/images/:imageId/primary`

**Command:** `SetPrimaryImageCommand`  
**Auth:** Required (Admin/Seller)

```typescript
Response: {
  imageId: UUID;
}
```

**Implementação:**

- Desmarca outras como primárias
- Marca a selecionada
- Invalida cache do produto

---

#### `DELETE /api/products/:productId/images/:imageId`

**Command:** `DeleteProductImageCommand`  
**Auth:** Required (Admin/Seller)

```typescript
Response: {
  success: boolean;
}
```

**Implementação:**

- Remove da tabela (CASCADE)
- Remove arquivo do storage (async)
- Se era primária, marca próxima como primária

---

## AdminProductController

**Base Path:** `/api/admin/products`  
**Descrição:** Operações administrativas de produtos

### Queries (Read)

#### `GET /api/admin/products/low-stock`

**Query:** `GetLowStockProductsQuery`  
**Auth:** Required (Admin)

```typescript
Query Params: {
  threshold?: number;
  page?: number;
  pageSize?: number;
}

Response: {
  items: Array<{
    id: UUID;
    name: string;
    sku: string;
    stock: number;
    stockReserved: number;
    lowStockThreshold: number;
  }>;
  total: number;
}
```

**Implementação:**

- Filtra `stock <= low_stock_threshold`
- Usado para alertas de reposição
- Sem cache (dados em tempo real)

---

#### `GET /api/admin/products/:id/events`

**Query:** `GetProductEventsQuery`  
**Auth:** Required (Admin)

```typescript
Response: {
  events: Array<{
    id: UUID;
    eventType: string;
    payload: object;
    status: string;
    createdAt: Date;
  }>;
}
```

**Implementação:**

- Lista eventos do outbox
- Útil para debugging
- Sem cache

---

### Commands (Write)

#### `POST /api/admin/products/:id/stock/adjust`

**Command:** `AdjustStockCommand`  
**Auth:** Required (Admin/Stock Manager)

```typescript
Request Body: {
  adjustment: number;     // Pode ser negativo
  reason: string;
  reference?: string;     // Nota fiscal, etc
}

Response: {
  productId: UUID;
  oldStock: number;
  newStock: number;
}
```

**Implementação:**

- Ajuste manual de inventário
- Registra motivo e responsável
- Publica `StockAdjusted` event
- Cria registro de auditoria

---

#### `POST /api/admin/products/:id/stock/reserve`

**Command:** `ReserveStockCommand`  
**Auth:** Required (Internal/Order Service)

```typescript
Request Body: {
  quantity: number;
  orderId: UUID;
  expiresAt?: Date;
}

Response: {
  reservationId: UUID;
  available: number;
}
```

**Implementação:**

- Reserva estoque para pedido
- Idempotente usando orderId (inbox)
- Valida disponibilidade
- Publica `StockReserved`

---

#### `POST /api/admin/products/:id/stock/release`

**Command:** `ReleaseStockCommand`  
**Auth:** Required (Internal/Order Service)

```typescript
Request Body: {
  quantity: number;
  orderId: UUID;
  reason: 'CANCELLED' | 'EXPIRED' | 'REJECTED';
}

Response: {
  productId: UUID;
  released: number;
}
```

**Implementação:**

- Libera estoque reservado
- Chamado quando pedido cancela
- Publica `StockReleased`

---

#### `POST /api/admin/products/:id/stock/commit`

**Command:** `CommitStockCommand`  
**Auth:** Required (Internal/Order Service)

```typescript
Request Body: {
  quantity: number;
  orderId: UUID;
}

Response: {
  productId: UUID;
  committed: number;
}
```

**Implementação:**

- Confirma venda (decrementa stock)
- Chamado quando pagamento aprovado
- Publica `StockCommitted`

---

## AdminReviewController

**Base Path:** `/api/admin/reviews`  
**Descrição:** Moderação de reviews

### Queries (Read)

#### `GET /api/admin/reviews/pending`

**Query:** `GetPendingReviewsQuery`  
**Auth:** Required (Admin/Moderator)

```typescript
Query Params: {
  page?: number;
  pageSize?: number;
}

Response: {
  items: Array<{
    id: UUID;
    product: { id, name, image };
    user: { id, name, email };
    rating: number;
    title: string;
    comment: string;
    createdAt: Date;
    reportCount?: number;
  }>;
  total: number;
}
```

**Implementação:**

- Reviews aguardando moderação
- FIFO (mais antigas primeiro)
- Sem cache

---

### Commands (Write)

#### `POST /api/admin/reviews/:id/approve`

**Command:** `ApproveReviewCommand`  
**Auth:** Required (Admin/Moderator)

```typescript
Response: {
  reviewId: UUID;
  approvedAt: Date;
}
```

**Implementação:**

- Define `is_approved = true`
- Registra moderador
- Trigger atualiza stats do produto
- Invalida caches

---

#### `POST /api/admin/reviews/:id/reject`

**Command:** `RejectReviewCommand`  
**Auth:** Required (Admin/Moderator)

```typescript
Request Body: {
  reason: string;
}

Response: {
  reviewId: UUID;
}
```

**Implementação:**

- Soft delete
- Registra motivo
- Notifica usuário (opcional)
- Invalida caches

---

#### `POST /api/admin/reviews/:id/feature`

**Command:** `FeatureReviewCommand`  
**Auth:** Required (Admin)

```typescript
Response: {
  reviewId: UUID;
}
```

**Implementação:**

- Marca como `is_featured = true`
- Usado para destacar reviews úteis
- Limite de N reviews featured por produto

---

## Resumo de Endpoints

| Controller              | Queries | Commands | Total  | Auth    |
| ----------------------- | ------- | -------- | ------ | ------- |
| ProductController       | 5       | 4        | 9      | Parcial |
| CategoryController      | 4       | 2        | 6      | Parcial |
| ProductReviewController | 3       | 4        | 7      | Sim     |
| FavoriteController      | 2       | 2        | 4      | Sim     |
| ProductImageController  | 1       | 3        | 4      | Parcial |
| AdminProductController  | 2       | 4        | 6      | Admin   |
| AdminReviewController   | 1       | 3        | 4      | Admin   |
| **TOTAL**               | **18**  | **22**   | **40** | -       |

---

## Padrões de Implementação

### 🔐 Autenticação

```typescript
// Public endpoints
GET /api/products/*
GET /api/categories/*

// User authenticated
POST /api/favorites
POST /api/reviews
GET /api/users/me/*

// Admin only
POST /api/admin/*
PUT /api/admin/*
DELETE /api/admin/*
```

### 📊 Response Format

```typescript
// Success (Query)
{
  data: T,
  meta?: {
    page, pageSize, total, totalPages
  }
}

// Success (Command)
{
  id: UUID,
  message?: string
}

// Error
{
  error: {
    code: string,
    message: string,
    details?: object
  }
}
```

### 🚀 Cache Strategy

```typescript
// Long cache (30min)
- Category tree
- Category list

// Medium cache (10-15min)
- Featured products
- Product images
- Review stats

// Short cache (5min)
- Product details
- Product search
- Category products

// No cache
- Stock info (real-time)
- User favorites
- Admin queries
```

### 🔄 Event Flow

```
Command → Domain → DB → Trigger → Outbox → Message Broker → Event Handlers
```

---

**API Documentation - Catalog Service v1.0**  
_RESTful API organized by Controllers with CQRS pattern_
