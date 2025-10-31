-- ========================================
-- CATALOG SERVICE - DATABASE SCHEMA
-- ========================================
-- Responsável por: Produtos, Categorias, Reviews, Favoritos

-- ========================================
-- EXTENSIONS
-- ========================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm"; -- Para busca full-text

-- ========================================
-- TABLES
-- ========================================

CREATE TABLE categories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(120) UNIQUE NOT NULL,
    description TEXT,
    parent_id UUID REFERENCES categories(id) ON DELETE SET NULL,
    is_active BOOLEAN DEFAULT TRUE,
    display_order INT DEFAULT 0,
    metadata JSONB DEFAULT '{}',
    version INT DEFAULT 1,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now()
);

CREATE TABLE products (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(150) NOT NULL,
    slug VARCHAR(180) UNIQUE NOT NULL,
    description TEXT,
    short_description VARCHAR(500),
    
    -- Pricing
    price DECIMAL(10,2) NOT NULL CHECK (price >= 0),
    compare_at_price DECIMAL(10,2) CHECK (compare_at_price >= 0),
    cost_price DECIMAL(10,2) CHECK (cost_price >= 0),
    
    -- Inventory
    stock INT DEFAULT 0 CHECK (stock >= 0),
    stock_reserved INT DEFAULT 0 CHECK (stock_reserved >= 0),
    low_stock_threshold INT DEFAULT 10,
    
    -- Categorization
    category_id UUID REFERENCES categories(id) ON DELETE SET NULL,
    
    -- SEO
    meta_title VARCHAR(200),
    meta_description VARCHAR(500),
    
    -- Attributes
    weight_kg DECIMAL(10,3),
    dimensions_cm VARCHAR(50), -- "10x20x30"
    sku VARCHAR(100) UNIQUE,
    barcode VARCHAR(100),
    
    -- Status
    is_active BOOLEAN DEFAULT TRUE,
    is_featured BOOLEAN DEFAULT FALSE,
    
    -- Stats (denormalized for performance)
    view_count INT DEFAULT 0,
    favorite_count INT DEFAULT 0,
    review_count INT DEFAULT 0,
    review_avg_rating DECIMAL(3,2) DEFAULT 0,
    
    version INT DEFAULT 1,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now(),
    deleted_at TIMESTAMP
);

CREATE TABLE product_images (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    url TEXT NOT NULL,
    thumbnail_url TEXT,
    alt_text VARCHAR(255),
    display_order INT DEFAULT 0,
    is_primary BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now()
);

-- Tabela de favoritos (user_id é referência externa ao Identity Service)
CREATE TABLE favorite_products (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL, -- External reference (Identity Service)
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    created_at TIMESTAMP DEFAULT now(),
    UNIQUE(user_id, product_id)
);

-- Reviews
CREATE TABLE product_reviews (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    user_id UUID NOT NULL, -- External reference (Identity Service)
    
    rating INT NOT NULL CHECK (rating >= 1 AND rating <= 5),
    title VARCHAR(200),
    comment TEXT,
    
    is_verified_purchase BOOLEAN DEFAULT FALSE,
    helpful_count INT DEFAULT 0,
    unhelpful_count INT DEFAULT 0,
    
    -- Moderação
    is_approved BOOLEAN DEFAULT FALSE,
    is_featured BOOLEAN DEFAULT FALSE,
    moderated_at TIMESTAMP,
    moderated_by UUID, -- Admin user
    
    version INT DEFAULT 1,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now(),
    deleted_at TIMESTAMP
);

-- Votos em reviews (evita votos duplicados)
CREATE TABLE review_votes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    review_id UUID NOT NULL REFERENCES product_reviews(id) ON DELETE CASCADE,
    user_id UUID NOT NULL,
    is_helpful BOOLEAN NOT NULL,
    created_at TIMESTAMP DEFAULT now(),
    UNIQUE(review_id, user_id)
);

-- ========================================
-- OUTBOX PATTERN
-- ========================================

CREATE TYPE outbox_status AS ENUM ('PENDING', 'PROCESSING', 'PUBLISHED', 'FAILED');

CREATE TABLE outbox_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_id UUID NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL, -- PRODUCT, CATEGORY
    event_type VARCHAR(100) NOT NULL, -- ProductCreated, StockReserved, PriceChanged, etc
    event_version INT DEFAULT 1,
    payload JSONB NOT NULL,
    metadata JSONB DEFAULT '{}',
    status outbox_status DEFAULT 'PENDING',
    retry_count INT DEFAULT 0,
    max_retries INT DEFAULT 3,
    error_message TEXT,
    published_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now()
);

-- ========================================
-- INBOX PATTERN
-- ========================================

CREATE TABLE inbox_events (
    id UUID PRIMARY KEY,
    event_type VARCHAR(100) NOT NULL,
    aggregate_id UUID NOT NULL,
    processed_at TIMESTAMP DEFAULT now(),
    created_at TIMESTAMP DEFAULT now()
);

-- Armazena eventos recebidos de outros serviços
-- Ex: OrderCompleted -> marca is_verified_purchase = TRUE
CREATE TABLE received_events (
    id UUID PRIMARY KEY,
    event_type VARCHAR(100) NOT NULL,
    source_service VARCHAR(100) NOT NULL,
    payload JSONB NOT NULL,
    processed BOOLEAN DEFAULT FALSE,
    processed_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT now()
);

-- ========================================
-- INDEXES
-- ========================================

-- Categories
CREATE INDEX idx_categories_slug ON categories(slug);
CREATE INDEX idx_categories_parent_id ON categories(parent_id);
CREATE INDEX idx_categories_active ON categories(is_active);

-- Products
CREATE INDEX idx_products_slug ON products(slug) WHERE deleted_at IS NULL;
CREATE INDEX idx_products_category_id ON products(category_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_products_active ON products(is_active) WHERE deleted_at IS NULL;
CREATE INDEX idx_products_featured ON products(is_featured) WHERE is_featured = TRUE AND deleted_at IS NULL;
CREATE INDEX idx_products_sku ON products(sku) WHERE deleted_at IS NULL;
CREATE INDEX idx_products_price ON products(price) WHERE deleted_at IS NULL;
CREATE INDEX idx_products_stock ON products(stock) WHERE deleted_at IS NULL;

-- Full-text search
CREATE INDEX idx_products_name_trgm ON products USING gin(name gin_trgm_ops);
CREATE INDEX idx_products_description_trgm ON products USING gin(description gin_trgm_ops);

-- Product Images
CREATE INDEX idx_product_images_product_id ON product_images(product_id);
CREATE INDEX idx_product_images_primary ON product_images(product_id, is_primary) WHERE is_primary = TRUE;

-- Favorites
CREATE INDEX idx_favorite_products_user_id ON favorite_products(user_id);
CREATE INDEX idx_favorite_products_product_id ON favorite_products(product_id);

-- Reviews
CREATE INDEX idx_product_reviews_product_id ON product_reviews(product_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_product_reviews_user_id ON product_reviews(user_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_product_reviews_approved ON product_reviews(is_approved) WHERE is_approved = TRUE AND deleted_at IS NULL;
CREATE INDEX idx_product_reviews_rating ON product_reviews(rating);

-- Review Votes
CREATE INDEX idx_review_votes_review_id ON review_votes(review_id);
CREATE INDEX idx_review_votes_user_id ON review_votes(user_id);

-- Outbox/Inbox
CREATE INDEX idx_outbox_events_status ON outbox_events(status) WHERE status IN ('PENDING', 'FAILED');
CREATE INDEX idx_outbox_events_created_at ON outbox_events(created_at);
CREATE INDEX idx_inbox_events_aggregate ON inbox_events(aggregate_id, event_type);
CREATE INDEX idx_received_events_processed ON received_events(processed) WHERE processed = FALSE;

-- ========================================
-- FUNCTIONS
-- ========================================

CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION increment_version()
RETURNS TRIGGER AS $$
BEGIN
    NEW.version = OLD.version + 1;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Atualiza estatísticas do produto quando review é criada/atualizada
CREATE OR REPLACE FUNCTION update_product_review_stats()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE products
    SET 
        review_count = (
            SELECT COUNT(*) 
            FROM product_reviews 
            WHERE product_id = COALESCE(NEW.product_id, OLD.product_id) 
            AND is_approved = TRUE 
            AND deleted_at IS NULL
        ),
        review_avg_rating = (
            SELECT COALESCE(AVG(rating), 0) 
            FROM product_reviews 
            WHERE product_id = COALESCE(NEW.product_id, OLD.product_id) 
            AND is_approved = TRUE 
            AND deleted_at IS NULL
        )
    WHERE id = COALESCE(NEW.product_id, OLD.product_id);
    
    RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

-- Atualiza contador de favoritos
CREATE OR REPLACE FUNCTION update_favorite_count()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE products 
        SET favorite_count = favorite_count + 1 
        WHERE id = NEW.product_id;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE products 
        SET favorite_count = favorite_count - 1 
        WHERE id = OLD.product_id;
    END IF;
    RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

-- Publica evento quando produto é criado/atualizado
CREATE OR REPLACE FUNCTION publish_product_event()
RETURNS TRIGGER AS $$
DECLARE
    event_type_name VARCHAR(100);
    event_payload JSONB;
BEGIN
    IF TG_OP = 'INSERT' THEN
        event_type_name := 'ProductCreated';
        event_payload := to_jsonb(NEW);
    ELSIF TG_OP = 'UPDATE' THEN
        -- Detecta tipo específico de mudança
        IF OLD.price != NEW.price THEN
            event_type_name := 'ProductPriceChanged';
            event_payload := jsonb_build_object(
                'product_id', NEW.id,
                'old_price', OLD.price,
                'new_price', NEW.price
            );
        ELSIF OLD.stock != NEW.stock THEN
            event_type_name := 'ProductStockChanged';
            event_payload := jsonb_build_object(
                'product_id', NEW.id,
                'old_stock', OLD.stock,
                'new_stock', NEW.stock
            );
        ELSE
            event_type_name := 'ProductUpdated';
            event_payload := jsonb_build_object(
                'before', to_jsonb(OLD),
                'after', to_jsonb(NEW)
            );
        END IF;
    END IF;

    INSERT INTO outbox_events (
        aggregate_id,
        aggregate_type,
        event_type,
        payload,
        metadata
    ) VALUES (
        NEW.id,
        'PRODUCT',
        event_type_name,
        event_payload,
        jsonb_build_object('correlation_id', uuid_generate_v4())
    );

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- TRIGGERS
-- ========================================

-- Updated At
CREATE TRIGGER update_categories_updated_at 
    BEFORE UPDATE ON categories 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_products_updated_at 
    BEFORE UPDATE ON products 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_product_reviews_updated_at 
    BEFORE UPDATE ON product_reviews 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Version Control
CREATE TRIGGER update_products_version 
    BEFORE UPDATE ON products 
    FOR EACH ROW EXECUTE FUNCTION increment_version();

CREATE TRIGGER update_product_reviews_version 
    BEFORE UPDATE ON product_reviews 
    FOR EACH ROW EXECUTE FUNCTION increment_version();

-- Stats
CREATE TRIGGER update_review_stats_on_insert 
    AFTER INSERT ON product_reviews 
    FOR EACH ROW EXECUTE FUNCTION update_product_review_stats();

CREATE TRIGGER update_review_stats_on_update 
    AFTER UPDATE ON product_reviews 
    FOR EACH ROW EXECUTE FUNCTION update_product_review_stats();

CREATE TRIGGER update_review_stats_on_delete 
    AFTER DELETE ON product_reviews 
    FOR EACH ROW EXECUTE FUNCTION update_product_review_stats();

CREATE TRIGGER update_favorite_count_on_insert 
    AFTER INSERT ON favorite_products 
    FOR EACH ROW EXECUTE FUNCTION update_favorite_count();

CREATE TRIGGER update_favorite_count_on_delete 
    AFTER DELETE ON favorite_products 
    FOR EACH ROW EXECUTE FUNCTION update_favorite_count();

-- Event Publishing
CREATE TRIGGER publish_product_created 
    AFTER INSERT ON products 
    FOR EACH ROW EXECUTE FUNCTION publish_product_event();

CREATE TRIGGER publish_product_updated 
    AFTER UPDATE ON products 
    FOR EACH ROW EXECUTE FUNCTION publish_product_event();

-- ========================================
-- VIEWS (Read Models)
-- ========================================

-- View de produtos com informações agregadas
CREATE OR REPLACE VIEW product_catalog AS
SELECT 
    p.id,
    p.name,
    p.slug,
    p.description,
    p.short_description,
    p.price,
    p.compare_at_price,
    p.stock,
    p.is_active,
    p.is_featured,
    p.review_count,
    p.review_avg_rating,
    p.favorite_count,
    
    -- Categoria
    jsonb_build_object(
        'id', c.id,
        'name', c.name,
        'slug', c.slug
    ) AS category,
    
    -- Imagem principal
    (
        SELECT url 
        FROM product_images 
        WHERE product_id = p.id AND is_primary = TRUE 
        LIMIT 1
    ) AS primary_image,
    
    -- Todas as imagens
    (
        SELECT jsonb_agg(
            jsonb_build_object(
                'url', url,
                'thumbnail_url', thumbnail_url,
                'alt_text', alt_text
            ) ORDER BY display_order
        )
        FROM product_images
        WHERE product_id = p.id
    ) AS images,
    
    p.created_at,
    p.updated_at
    
FROM products p
LEFT JOIN categories c ON c.id = p.category_id
WHERE p.deleted_at IS NULL;

-- ========================================
-- COMMENTS
-- ========================================

COMMENT ON TABLE products IS 'Catálogo de produtos do e-commerce';
COMMENT ON COLUMN products.stock_reserved IS 'Estoque reservado por carrinhos/pedidos não finalizados';
COMMENT ON TABLE outbox_events IS 'Eventos a serem publicados para outros serviços';
COMMENT ON TABLE received_events IS 'Eventos recebidos de outros serviços (OrderCompleted, etc)';