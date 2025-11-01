-- ========================================
-- CATALOG SERVICE - DATABASE SCHEMA
-- ========================================
-- Responsável por gerenciar produtos, categorias, reviews e favoritos

-- ========================================
-- EXTENSIONS
-- ========================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp"; -- Gera UUIDs únicos
CREATE EXTENSION IF NOT EXISTS "pg_trgm"; -- Suporte para busca full-text com trigramas

-- ========================================
-- TABLES
-- ========================================

-- Tabela de categorias de produtos
CREATE TABLE categories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(), -- Identificador único da categoria
    name VARCHAR(100) NOT NULL, -- Nome da categoria
    slug VARCHAR(120) UNIQUE NOT NULL, -- URL amigável da categoria
    description TEXT, -- Descrição da categoria
    parent_id UUID REFERENCES categories(id) ON DELETE SET NULL, -- Categoria pai para hierarquia
    is_active BOOLEAN DEFAULT TRUE, -- Flag para ativar/desativar categoria
    display_order INT DEFAULT 0, -- Ordem de exibição
    metadata JSONB DEFAULT '{}', -- Informações adicionais flexíveis
    version INT DEFAULT 1, -- Controle de versão da categoria
    created_at TIMESTAMP DEFAULT now(), -- Data de criação
    updated_at TIMESTAMP DEFAULT now() -- Data de última atualização
);

-- Tabela de produtos
CREATE TABLE products (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(), -- Identificador único do produto
    name VARCHAR(150) NOT NULL, -- Nome do produto
    slug VARCHAR(180) UNIQUE NOT NULL, -- URL amigável do produto
    description TEXT, -- Descrição completa do produto
    short_description VARCHAR(500), -- Descrição resumida

    -- Preços
    price DECIMAL(10,2) NOT NULL CHECK (price >= 0), -- Preço atual do produto
    compare_at_price DECIMAL(10,2) CHECK (compare_at_price >= 0), -- Preço original (para promoções)
    cost_price DECIMAL(10,2) CHECK (cost_price >= 0), -- Custo do produto para cálculo de margem

    -- Estoque
    stock INT DEFAULT 0 CHECK (stock >= 0), -- Quantidade em estoque
    stock_reserved INT DEFAULT 0 CHECK (stock_reserved >= 0), -- Estoque reservado por carrinhos/pedidos
    low_stock_threshold INT DEFAULT 10, -- Limite para alerta de estoque baixo

    -- Categorias
    category_id UUID REFERENCES categories(id) ON DELETE SET NULL, -- Categoria do produto

    -- SEO
    meta_title VARCHAR(200), -- Título SEO
    meta_description VARCHAR(500), -- Descrição SEO

    -- Atributos físicos
    weight_kg DECIMAL(10,3), -- Peso em kg
    dimensions_cm VARCHAR(50), -- Dimensões (ex: 10x20x30)
    sku VARCHAR(100) UNIQUE, -- Código SKU único
    barcode VARCHAR(100), -- Código de barras

    -- Status
    is_active BOOLEAN DEFAULT TRUE, -- Produto ativo
    is_featured BOOLEAN DEFAULT FALSE, -- Produto em destaque

    -- Estatísticas
    view_count INT DEFAULT 0, -- Quantidade de visualizações
    favorite_count INT DEFAULT 0, -- Quantidade de favoritos
    review_count INT DEFAULT 0, -- Quantidade de reviews aprovadas
    review_avg_rating DECIMAL(3,2) DEFAULT 0, -- Média das avaliações

    version INT DEFAULT 1, -- Controle de versão do produto
    created_at TIMESTAMP DEFAULT now(), -- Data de criação
    updated_at TIMESTAMP DEFAULT now(), -- Data de atualização
    deleted_at TIMESTAMP -- Data de exclusão lógica
);

-- Imagens dos produtos
CREATE TABLE product_images (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(), -- Identificador único da imagem
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE, -- Produto associado
    url TEXT NOT NULL, -- URL da imagem
    thumbnail_url TEXT, -- URL da miniatura
    alt_text VARCHAR(255), -- Texto alternativo
    display_order INT DEFAULT 0, -- Ordem de exibição
    is_primary BOOLEAN DEFAULT FALSE, -- Marca se é a imagem principal
    created_at TIMESTAMP DEFAULT now(), -- Data de criação
    updated_at TIMESTAMP DEFAULT now() -- Data de atualização
);

-- Favoritos dos usuários
CREATE TABLE favorite_products (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(), -- Identificador único do favorito
    user_id UUID NOT NULL, -- Referência ao usuário (Identity Service)
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE, -- Produto favoritado
    created_at TIMESTAMP DEFAULT now(), -- Data de criação
    UNIQUE(user_id, product_id) -- Garante que o mesmo usuário não favoritará o mesmo produto duas vezes
);

-- Reviews de produtos
CREATE TABLE product_reviews (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(), -- Identificador único da review
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE, -- Produto associado
    user_id UUID NOT NULL, -- Referência ao usuário (Identity Service)
    
    rating INT NOT NULL CHECK (rating >= 1 AND rating <= 5), -- Nota da review
    title VARCHAR(200), -- Título da review
    comment TEXT, -- Comentário completo
    
    is_verified_purchase BOOLEAN DEFAULT FALSE, -- Confirma se a compra foi realizada
    helpful_count INT DEFAULT 0, -- Contador de votos positivos
    unhelpful_count INT DEFAULT 0, -- Contador de votos negativos
    
    -- Moderação
    is_approved BOOLEAN DEFAULT FALSE, -- Review aprovada
    is_featured BOOLEAN DEFAULT FALSE, -- Destaque na página
    moderated_at TIMESTAMP, -- Data de moderação
    moderated_by UUID, -- ID do moderador/admin
    
    version INT DEFAULT 1, -- Controle de versão
    created_at TIMESTAMP DEFAULT now(), -- Data de criação
    updated_at TIMESTAMP DEFAULT now(), -- Data de atualização
    deleted_at TIMESTAMP -- Data de exclusão lógica
);

-- Votos em reviews
CREATE TABLE review_votes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(), -- Identificador do voto
    review_id UUID NOT NULL REFERENCES product_reviews(id) ON DELETE CASCADE, -- Review associada
    user_id UUID NOT NULL, -- Usuário que votou
    is_helpful BOOLEAN NOT NULL, -- Se foi útil ou não
    created_at TIMESTAMP DEFAULT now(), -- Data do voto
    UNIQUE(review_id, user_id) -- Garante um voto por usuário por review
);

-- ========================================
-- OUTBOX PATTERN
-- ========================================
CREATE TYPE outbox_status AS ENUM ('PENDING', 'PROCESSING', 'PUBLISHED', 'FAILED'); -- Status do evento

CREATE TABLE outbox_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(), -- Identificador único do evento
    aggregate_id UUID NOT NULL, -- ID do agregado (produto/categoria)
    aggregate_type VARCHAR(100) NOT NULL, -- Tipo do agregado (PRODUCT, CATEGORY)
    event_type VARCHAR(100) NOT NULL, -- Tipo do evento (ProductCreated, PriceChanged)
    event_version INT DEFAULT 1, -- Versão do evento
    payload JSONB NOT NULL, -- Dados do evento
    metadata JSONB DEFAULT '{}', -- Metadados opcionais (ex: correlation_id)
    status outbox_status DEFAULT 'PENDING', -- Status do evento
    retry_count INT DEFAULT 0, -- Tentativas já realizadas
    max_retries INT DEFAULT 3, -- Máximo de tentativas
    error_message TEXT, -- Mensagem de erro se falhar
    published_at TIMESTAMP, -- Data de publicação
    created_at TIMESTAMP DEFAULT now(), -- Data de criação
    updated_at TIMESTAMP DEFAULT now() -- Data de atualização
);

-- ========================================
-- INBOX PATTERN
-- ========================================

CREATE TABLE inbox_events (
    id UUID PRIMARY KEY, -- Identificador único do evento
    event_type VARCHAR(100) NOT NULL, -- Tipo do evento
    aggregate_id UUID NOT NULL, -- Agregado relacionado
    processed_at TIMESTAMP DEFAULT now(), -- Data de processamento
    created_at TIMESTAMP DEFAULT now() -- Data de criação
);

-- Eventos recebidos de outros serviços
CREATE TABLE received_events (
    id UUID PRIMARY KEY, -- Identificador do evento
    event_type VARCHAR(100) NOT NULL, -- Tipo do evento
    source_service VARCHAR(100) NOT NULL, -- Serviço que enviou o evento
    payload JSONB NOT NULL, -- Dados do evento
    processed BOOLEAN DEFAULT FALSE, -- Indica se já foi processado
    processed_at TIMESTAMP, -- Data de processamento
    created_at TIMESTAMP DEFAULT now() -- Data de criação
);

-- ========================================
-- COMMENTS
-- ========================================
COMMENT ON TABLE products IS 'Catálogo de produtos do e-commerce';
COMMENT ON COLUMN products.stock_reserved IS 'Estoque reservado por carrinhos/pedidos não finalizados';
COMMENT ON TABLE outbox_events IS 'Eventos a serem publicados para outros serviços';
COMMENT ON TABLE received_events IS 'Eventos recebidos de outros serviços (OrderCompleted, etc)';
