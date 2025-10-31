-- ========================================
-- CART SERVICE - DATABASE SCHEMA
-- ========================================
-- Responsável por: Carrinhos, Itens do Carrinho, Carrinhos Abandonados

-- ========================================
-- EXTENSIONS
-- ========================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ========================================
-- ENUMS
-- ========================================

CREATE TYPE cart_status AS ENUM ('OPEN', 'ABANDONED', 'CONVERTED', 'EXPIRED', 'MERGED');
CREATE TYPE cart_event_type AS ENUM (
    'CREATED',
    'ITEM_ADDED', 
    'ITEM_REMOVED', 
    'ITEM_UPDATED',
    'ITEM_QUANTITY_INCREASED',
    'ITEM_QUANTITY_DECREASED',
    'CART_ABANDONED', 
    'CART_CLEARED',
    'CART_CONVERTED',
    'CART_EXPIRED',
    'CART_MERGED'
);

-- ========================================
-- TABLES
-- ========================================

CREATE TABLE carts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID, -- NULL para carrinhos guest/anônimos
    session_id VARCHAR(100) NOT NULL,
    
    status cart_status DEFAULT 'OPEN',
    
    -- Totais (denormalized para performance)
    items_count INT DEFAULT 0,
    subtotal DECIMAL(10,2) DEFAULT 0 CHECK (subtotal >= 0),
    
    -- TTL
    expires_at TIMESTAMP,
    last_activity_at TIMESTAMP DEFAULT now(),
    
    -- Conversão
    converted_to_order_id UUID, -- External reference (Order Service)
    converted_at TIMESTAMP,
    
    version INT DEFAULT 1,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now()
);

CREATE TABLE cart_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    cart_id UUID NOT NULL REFERENCES carts(id) ON DELETE CASCADE,
    product_id UUID NOT NULL, -- External reference (Catalog Service)
    
    -- Product Snapshot (anti-corruption layer)
    product_snapshot JSONB NOT NULL, 
    -- {
    --   "name": "Product Name",
    --   "slug": "product-slug",
    --   "description": "Short description",
    --   "price": 99.99,
    --   "image_url": "https://...",
    --   "category": {"id": "...", "name": "..."},
    --   "sku": "ABC123"
    -- }
    
    quantity INT NOT NULL CHECK (quantity > 0),
    unit_price DECIMAL(10,2) NOT NULL CHECK (unit_price >= 0),
    subtotal DECIMAL(10,2) GENERATED ALWAYS AS (quantity * unit_price) STORED,
    
    -- Metadata
    added_at TIMESTAMP DEFAULT now(),
    version INT DEFAULT 1,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now(),
    
    UNIQUE(cart_id, product_id)
);

-- Event Sourcing para rastreabilidade completa
CREATE TABLE cart_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    cart_id UUID NOT NULL,
    event_type cart_event_type NOT NULL,
    event_data JSONB NOT NULL,
    -- {
    --   "product_id": "...",
    --   "quantity": 2,
    --   "price": 99.99,
    --   "user_id": "...",
    --   "session_id": "..."
    -- }
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP DEFAULT now()
);

-- Carrinhos Abandonados (para recovery campaigns)
CREATE TABLE abandoned_carts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    cart_id UUID NOT NULL UNIQUE,
    user_id UUID, -- NULL se guest
    session_id VARCHAR(100) NOT NULL,
    
    -- Snapshot completo para análise
    cart_snapshot JSONB NOT NULL,
    -- {
    --   "items": [...],
    --   "subtotal": 299.99,
    --   "items_count": 3,
    --   "user_email": "user@example.com"
    -- }
    
    -- Recovery tracking
    detected_at TIMESTAMP DEFAULT now(),
    recovery_email_sent BOOLEAN DEFAULT FALSE,
    recovery_email_sent_at TIMESTAMP,
    recovery_email_opened BOOLEAN DEFAULT FALSE,
    recovery_email_clicked BOOLEAN DEFAULT FALSE,
    
    recovered BOOLEAN DEFAULT FALSE,
    recovered_at TIMESTAMP,
    recovery_order_id UUID, -- External reference
    
    -- Analytics
    abandonment_reason VARCHAR(100), -- PRICE, SHIPPING, CHECKOUT_COMPLEXITY, etc
    device_type VARCHAR(50),
    browser VARCHAR(50),
    
    created_at TIMESTAMP DEFAULT now()
);

-- ========================================
-- OUTBOX PATTERN
-- ========================================

CREATE TYPE outbox_status AS ENUM ('PENDING', 'PROCESSING', 'PUBLISHED', 'FAILED');

CREATE TABLE outbox_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_id UUID NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL, -- CART
    event_type VARCHAR(100) NOT NULL,
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

-- Eventos recebidos de outros serviços
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

-- Carts
CREATE INDEX idx_carts_user_id ON carts(user_id) WHERE user_id IS NOT NULL;
CREATE INDEX idx_carts_session_id ON carts(session_id);
CREATE INDEX idx_carts_status ON carts(status);
CREATE INDEX idx_carts_expires_at ON carts(expires_at) WHERE status = 'OPEN';
CREATE INDEX idx_carts_last_activity ON carts(last_activity_at);
CREATE INDEX idx_carts_open_user ON carts(user_id, status) WHERE status = 'OPEN';

-- Cart Items
CREATE INDEX idx_cart_items_cart_id ON cart_items(cart_id);
CREATE INDEX idx_cart_items_product_id ON cart_items(product_id);

-- Cart Events (Event Sourcing)
CREATE INDEX idx_cart_events_cart_id ON cart_events(cart_id);
CREATE INDEX idx_cart_events_type ON cart_events(event_type);
CREATE INDEX idx_cart_events_created_at ON cart_events(created_at);

-- Abandoned Carts
CREATE INDEX idx_abandoned_carts_user_id ON abandoned_carts(user_id) WHERE user_id IS NOT NULL;
CREATE INDEX idx_abandoned_carts_detected_at ON abandoned_carts(detected_at);
CREATE INDEX idx_abandoned_carts_recovery ON abandoned_carts(recovered) WHERE recovered = FALSE;
CREATE INDEX idx_abandoned_carts_email_sent ON abandoned_carts(recovery_email_sent) WHERE recovery_email_sent = FALSE;

-- Outbox/Inbox
CREATE INDEX idx_outbox_events_status ON outbox_events(status) WHERE status IN ('PENDING', 'FAILED');
CREATE INDEX idx_outbox_events_created_at ON outbox_events(created_at);
CREATE INDEX idx_inbox_events_aggregate ON inbox_events(aggregate_id, event_type);
CREATE INDEX idx_received_events_processed ON received_events(processed) WHERE processed = FALSE;

-- ========================================
-- FUNCTIONS
-- ========================================

CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION increment_version()
RETURNS TRIGGER AS $
BEGIN
    NEW.version = OLD.version + 1;
    RETURN NEW;
END;
$ LANGUAGE plpgsql;

-- Atualiza totais do carrinho
CREATE OR REPLACE FUNCTION update_cart_totals()
RETURNS TRIGGER AS $
BEGIN
    UPDATE carts
    SET 
        items_count = (
            SELECT COALESCE(SUM(quantity), 0)
            FROM cart_items
            WHERE cart_id = COALESCE(NEW.cart_id, OLD.cart_id)
        ),
        subtotal = (
            SELECT COALESCE(SUM(subtotal), 0)
            FROM cart_items
            WHERE cart_id = COALESCE(NEW.cart_id, OLD.cart_id)
        ),
        last_activity_at = now()
    WHERE id = COALESCE(NEW.cart_id, OLD.cart_id);
    
    RETURN COALESCE(NEW, OLD);
END;
$ LANGUAGE plpgsql;

-- Registra evento no event store
CREATE OR REPLACE FUNCTION log_cart_event()
RETURNS TRIGGER AS $
DECLARE
    event_type_val cart_event_type;
    event_data_val JSONB;
BEGIN
    IF TG_OP = 'INSERT' THEN
        event_type_val := 'ITEM_ADDED';
        event_data_val := jsonb_build_object(
            'product_id', NEW.product_id,
            'quantity', NEW.quantity,
            'unit_price', NEW.unit_price,
            'subtotal', NEW.subtotal
        );
    ELSIF TG_OP = 'UPDATE' THEN
        IF NEW.quantity > OLD.quantity THEN
            event_type_val := 'ITEM_QUANTITY_INCREASED';
        ELSIF NEW.quantity < OLD.quantity THEN
            event_type_val := 'ITEM_QUANTITY_DECREASED';
        ELSE
            event_type_val := 'ITEM_UPDATED';
        END IF;
        
        event_data_val := jsonb_build_object(
            'product_id', NEW.product_id,
            'old_quantity', OLD.quantity,
            'new_quantity', NEW.quantity,
            'unit_price', NEW.unit_price
        );
    ELSIF TG_OP = 'DELETE' THEN
        event_type_val := 'ITEM_REMOVED';
        event_data_val := jsonb_build_object(
            'product_id', OLD.product_id,
            'quantity', OLD.quantity,
            'unit_price', OLD.unit_price
        );
    END IF;

    INSERT INTO cart_events (cart_id, event_type, event_data)
    VALUES (
        COALESCE(NEW.cart_id, OLD.cart_id),
        event_type_val,
        event_data_val
    );

    RETURN COALESCE(NEW, OLD);
END;
$ LANGUAGE plpgsql;

-- Publica evento no outbox
CREATE OR REPLACE FUNCTION publish_cart_event()
RETURNS TRIGGER AS $
DECLARE
    event_type_name VARCHAR(100);
BEGIN
    IF TG_OP = 'INSERT' THEN
        event_type_name := 'CartCreated';
    ELSIF TG_OP = 'UPDATE' THEN
        IF NEW.status = 'CONVERTED' AND OLD.status != 'CONVERTED' THEN
            event_type_name := 'CartConverted';
        ELSIF NEW.status = 'ABANDONED' AND OLD.status != 'ABANDONED' THEN
            event_type_name := 'CartAbandoned';
        ELSE
            event_type_name := 'CartUpdated';
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
        'CART',
        event_type_name,
        to_jsonb(NEW),
        jsonb_build_object(
            'user_id', NEW.user_id,
            'session_id', NEW.session_id,
            'correlation_id', uuid_generate_v4()
        )
    );

    RETURN NEW;
END;
$ LANGUAGE plpgsql;

-- Detecta carrinhos abandonados
CREATE OR REPLACE FUNCTION detect_abandoned_carts()
RETURNS void AS $
BEGIN
    INSERT INTO abandoned_carts (
        cart_id,
        user_id,
        session_id,
        cart_snapshot,
        detected_at
    )
    SELECT 
        c.id,
        c.user_id,
        c.session_id,
        jsonb_build_object(
            'cart_id', c.id,
            'items_count', c.items_count,
            'subtotal', c.subtotal,
            'items', (
                SELECT jsonb_agg(
                    jsonb_build_object(
                        'product_id', ci.product_id,
                        'product_name', ci.product_snapshot->>'name',
                        'quantity', ci.quantity,
                        'unit_price', ci.unit_price,
                        'subtotal', ci.subtotal
                    )
                )
                FROM cart_items ci
                WHERE ci.cart_id = c.id
            )
        ),
        now()
    FROM carts c
    WHERE c.status = 'OPEN'
    AND c.items_count > 0
    AND c.last_activity_at < now() - INTERVAL '1 hour'
    AND NOT EXISTS (
        SELECT 1 FROM abandoned_carts ac WHERE ac.cart_id = c.id
    )
    ON CONFLICT (cart_id) DO NOTHING;

    -- Atualiza status dos carrinhos
    UPDATE carts
    SET status = 'ABANDONED'
    WHERE id IN (
        SELECT cart_id FROM abandoned_carts WHERE detected_at >= now() - INTERVAL '1 minute'
    );
END;
$ LANGUAGE plpgsql;

-- ========================================
-- TRIGGERS
-- ========================================

-- Updated At
CREATE TRIGGER update_carts_updated_at 
    BEFORE UPDATE ON carts 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_cart_items_updated_at 
    BEFORE UPDATE ON cart_items 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Version Control
CREATE TRIGGER update_carts_version 
    BEFORE UPDATE ON carts 
    FOR EACH ROW EXECUTE FUNCTION increment_version();

CREATE TRIGGER update_cart_items_version 
    BEFORE UPDATE ON cart_items 
    FOR EACH ROW EXECUTE FUNCTION increment_version();

-- Cart Totals
CREATE TRIGGER update_cart_totals_on_insert 
    AFTER INSERT ON cart_items 
    FOR EACH ROW EXECUTE FUNCTION update_cart_totals();

CREATE TRIGGER update_cart_totals_on_update 
    AFTER UPDATE ON cart_items 
    FOR EACH ROW EXECUTE FUNCTION update_cart_totals();

CREATE TRIGGER update_cart_totals_on_delete 
    AFTER DELETE ON cart_items 
    FOR EACH ROW EXECUTE FUNCTION update_cart_totals();

-- Event Logging
CREATE TRIGGER log_cart_event_on_insert 
    AFTER INSERT ON cart_items 
    FOR EACH ROW EXECUTE FUNCTION log_cart_event();

CREATE TRIGGER log_cart_event_on_update 
    AFTER UPDATE ON cart_items 
    FOR EACH ROW EXECUTE FUNCTION log_cart_event();

CREATE TRIGGER log_cart_event_on_delete 
    AFTER DELETE ON cart_items 
    FOR EACH ROW EXECUTE FUNCTION log_cart_event();

-- Outbox Publishing
CREATE TRIGGER publish_cart_created 
    AFTER INSERT ON carts 
    FOR EACH ROW EXECUTE FUNCTION publish_cart_event();

CREATE TRIGGER publish_cart_updated 
    AFTER UPDATE ON carts 
    FOR EACH ROW EXECUTE FUNCTION publish_cart_event();

-- ========================================
-- VIEWS (Read Models)
-- ========================================

CREATE OR REPLACE VIEW active_carts AS
SELECT 
    c.id,
    c.user_id,
    c.session_id,
    c.status,
    c.items_count,
    c.subtotal,
    c.last_activity_at,
    c.created_at,
    
    -- Itens do carrinho
    (
        SELECT jsonb_agg(
            jsonb_build_object(
                'id', ci.id,
                'product_id', ci.product_id,
                'product', ci.product_snapshot,
                'quantity', ci.quantity,
                'unit_price', ci.unit_price,
                'subtotal', ci.subtotal
            )
        )
        FROM cart_items ci
        WHERE ci.cart_id = c.id
    ) AS items
    
FROM carts c
WHERE c.status = 'OPEN'
AND c.expires_at > now();

-- ========================================
-- SCHEDULED JOBS (usar pg_cron ou executar via cron/scheduler)
-- ========================================

-- Detectar carrinhos abandonados (executar a cada 15 minutos)
-- SELECT detect_abandoned_carts();

-- Expirar carrinhos antigos
-- UPDATE carts SET status = 'EXPIRED' 
-- WHERE status = 'OPEN' AND expires_at < now();

-- ========================================
-- COMMENTS
-- ========================================

COMMENT ON TABLE carts IS 'Carrinhos de compras - suporta usuários autenticados e guests';
COMMENT ON TABLE cart_items IS 'Itens do carrinho com snapshot do produto';
COMMENT ON TABLE cart_events IS 'Event sourcing - histórico completo de ações no carrinho';
COMMENT ON TABLE abandoned_carts IS 'Carrinhos abandonados para campanhas de recuperação';
COMMENT ON COLUMN cart_items.product_snapshot IS 'Snapshot do produto no momento da adição (evita joins cross-service)';
COMMENT ON FUNCTION detect_abandoned_carts IS 'Detecta e registra carrinhos abandonados (executar via cron)';