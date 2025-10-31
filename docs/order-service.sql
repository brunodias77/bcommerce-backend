-- ========================================
-- ORDER SERVICE - DATABASE SCHEMA
-- ========================================
-- Responsável por: Pedidos, Itens do Pedido, Tracking, Pagamentos

-- ========================================
-- EXTENSIONS
-- ========================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ========================================
-- ENUMS
-- ========================================

CREATE TYPE order_status AS ENUM (
    'CREATED',
    'PAYMENT_PENDING',
    'PAYMENT_AUTHORIZED',
    'PAID',
    'PROCESSING',
    'SHIPPED',
    'IN_TRANSIT',
    'OUT_FOR_DELIVERY',
    'DELIVERED',
    'CANCELLED',
    'REFUND_REQUESTED',
    'REFUNDED',
    'FAILED'
);

CREATE TYPE payment_method AS ENUM (
    'CREDIT_CARD',
    'DEBIT_CARD',
    'PIX',
    'BOLETO',
    'PAYPAL',
    'WALLET'
);

CREATE TYPE payment_status AS ENUM (
    'PENDING',
    'AUTHORIZED',
    'CAPTURED',
    'FAILED',
    'REFUNDED',
    'CANCELLED'
);

-- ========================================
-- TABLES
-- ========================================

CREATE TABLE orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_number VARCHAR(50) UNIQUE NOT NULL,
    user_id UUID NOT NULL, -- External reference (Identity Service)
    
    -- Referência ao carrinho original
    cart_id UUID, -- External reference (Cart Service)
    
    -- Valores
    subtotal DECIMAL(10,2) NOT NULL CHECK (subtotal >= 0),
    discount_amount DECIMAL(10,2) DEFAULT 0 CHECK (discount_amount >= 0),
    shipping_amount DECIMAL(10,2) DEFAULT 0 CHECK (shipping_amount >= 0),
    tax_amount DECIMAL(10,2) DEFAULT 0 CHECK (tax_amount >= 0),
    total DECIMAL(10,2) NOT NULL CHECK (total >= 0),
    
    -- Status
    status order_status DEFAULT 'CREATED',
    
    -- Snapshots (Anti-Corruption Layer - dados imutáveis)
    address_snapshot JSONB NOT NULL,
    -- {
    --   "street": "Rua Example",
    --   "number": "123",
    --   "city": "São Paulo",
    --   "state": "SP",
    --   "postal_code": "01234-567",
    --   "country": "BR",
    --   "recipient_name": "John Doe",
    --   "recipient_phone": "+5511999999999"
    -- }
    
    card_snapshot JSONB,
    -- {
    --   "last4": "1234",
    --   "brand": "VISA",
    --   "cardholder_name": "JOHN DOE"
    -- }
    
    coupon_snapshot JSONB,
    -- {
    --   "code": "SAVE20",
    --   "type": "PERCENTAGE",
    --   "discount_value": 20,
    --   "description": "20% off on all products"
    -- }
    
    -- Payment
    payment_method payment_method NOT NULL,
    payment_id VARCHAR(100), -- Gateway payment ID
    payment_status payment_status DEFAULT 'PENDING',
    
    -- Metadata
    notes TEXT,
    customer_notes TEXT,
    internal_notes TEXT,
    ip_address INET,
    user_agent TEXT,
    
    -- Timestamps
    confirmed_at TIMESTAMP,
    paid_at TIMESTAMP,
    shipped_at TIMESTAMP,
    delivered_at TIMESTAMP,
    cancelled_at TIMESTAMP,
    cancellation_reason TEXT,
    
    version INT DEFAULT 1,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now()
);

CREATE TABLE order_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id UUID NOT NULL, -- External reference (Catalog Service)
    
    -- Product Snapshot
    product_snapshot JSONB NOT NULL,
    -- {
    --   "name": "Product Name",
    --   "slug": "product-slug",
    --   "description": "Description",
    --   "image_url": "https://...",
    --   "category": {"id": "...", "name": "..."},
    --   "sku": "ABC123",
    --   "weight_kg": 1.5
    -- }
    
    quantity INT NOT NULL CHECK (quantity > 0),
    unit_price DECIMAL(10,2) NOT NULL CHECK (unit_price >= 0),
    discount_amount DECIMAL(10,2) DEFAULT 0 CHECK (discount_amount >= 0),
    total_price DECIMAL(10,2) NOT NULL CHECK (total_price >= 0),
    
    created_at TIMESTAMP DEFAULT now()
);

CREATE TABLE order_tracking (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    status order_status NOT NULL,
    
    -- Shipping details
    tracking_code VARCHAR(100),
    carrier VARCHAR(100),
    carrier_service VARCHAR(100),
    tracking_url TEXT,
    
    -- Location
    current_location VARCHAR(255),
    estimated_delivery TIMESTAMP,
    
    notes TEXT,
    metadata JSONB DEFAULT '{}',
    
    created_at TIMESTAMP DEFAULT now()
);

CREATE TABLE order_payments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    
    payment_method payment_method NOT NULL,
    payment_status payment_status DEFAULT 'PENDING',
    
    amount DECIMAL(10,2) NOT NULL CHECK (amount >= 0),
    currency VARCHAR(3) DEFAULT 'BRL',
    
    -- Gateway details
    gateway_provider VARCHAR(50), -- STRIPE, PAGARME, MERCADOPAGO
    gateway_transaction_id VARCHAR(100),
    gateway_authorization_code VARCHAR(100),
    gateway_response JSONB,
    
    -- Card details (se aplicável)
    card_last4 VARCHAR(4),
    card_brand VARCHAR(50),
    
    -- PIX/Boleto
    pix_qr_code TEXT,
    pix_qr_code_url TEXT,
    boleto_url TEXT,
    boleto_barcode VARCHAR(100),
    
    -- Timestamps
    authorized_at TIMESTAMP,
    captured_at TIMESTAMP,
    refunded_at TIMESTAMP,
    failed_at TIMESTAMP,
    error_message TEXT,
    
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now()
);

CREATE TABLE order_refunds (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    payment_id UUID REFERENCES order_payments(id) ON DELETE SET NULL,
    
    amount DECIMAL(10,2) NOT NULL CHECK (amount >= 0),
    reason VARCHAR(255) NOT NULL,
    notes TEXT,
    
    -- Gateway
    gateway_refund_id VARCHAR(100),
    gateway_response JSONB,
    
    status payment_status DEFAULT 'PENDING',
    processed_at TIMESTAMP,
    
    created_at TIMESTAMP DEFAULT now()
);

-- ========================================
-- OUTBOX PATTERN
-- ========================================

CREATE TYPE outbox_status AS ENUM ('PENDING', 'PROCESSING', 'PUBLISHED', 'FAILED');

CREATE TABLE outbox_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_id UUID NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL, -- ORDER, PAYMENT
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
-- SAGA ORCHESTRATION
-- ========================================

CREATE TYPE saga_status AS ENUM ('STARTED', 'COMPENSATING', 'COMPLETED', 'FAILED');
CREATE TYPE saga_step_status AS ENUM ('PENDING', 'SUCCEEDED', 'FAILED', 'COMPENSATED');

CREATE TABLE order_sagas (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    saga_type VARCHAR(100) NOT NULL, -- CREATE_ORDER, CANCEL_ORDER
    status saga_status DEFAULT 'STARTED',
    current_step INT DEFAULT 0,
    steps JSONB NOT NULL,
    -- [
    --   {"name": "ReserveInventory", "status": "SUCCEEDED", "service": "catalog"},
    --   {"name": "ProcessPayment", "status": "PENDING", "service": "payment"},
    --   {"name": "SendNotification", "status": "PENDING", "service": "notification"}
    -- ]
    error_message TEXT,
    completed_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now()
);

-- ========================================
-- INDEXES
-- ========================================

-- Orders
CREATE INDEX idx_orders_user_id ON orders(user_id);
CREATE INDEX idx_orders_status ON orders(status);
CREATE INDEX idx_orders_order_number ON orders(order_number);
CREATE INDEX idx_orders_created_at ON orders(created_at);
CREATE INDEX idx_orders_payment_status ON orders(payment_status);
CREATE INDEX idx_orders_cart_id ON orders(cart_id) WHERE cart_id IS NOT NULL;

-- Order Items
CREATE INDEX idx_order_items_order_id ON order_items(order_id);
CREATE INDEX idx_order_items_product_id ON order_items(product_id);

-- Order Tracking
CREATE INDEX idx_order_tracking_order_id ON order_tracking(order_id);
CREATE INDEX idx_order_tracking_status ON order_tracking(status);
CREATE INDEX idx_order_tracking_code ON order_tracking(tracking_code) WHERE tracking_code IS NOT NULL;
CREATE INDEX idx_order_tracking_created_at ON order_tracking(created_at);

-- Order Payments
CREATE INDEX idx_order_payments_order_id ON order_payments(order_id);
CREATE INDEX idx_order_payments_status ON order_payments(payment_status);
CREATE INDEX idx_order_payments_gateway_transaction ON order_payments(gateway_transaction_id) WHERE gateway_transaction_id IS NOT NULL;

-- Order Refunds
CREATE INDEX idx_order_refunds_order_id ON order_refunds(order_id);
CREATE INDEX idx_order_refunds_status ON order_refunds(status);

-- Sagas
CREATE INDEX idx_order_sagas_order_id ON order_sagas(order_id);
CREATE INDEX idx_order_sagas_status ON order_sagas(status) WHERE status IN ('STARTED', 'COMPENSATING');

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

-- Gera número de pedido sequencial
CREATE SEQUENCE order_number_seq START 1000;

CREATE OR REPLACE FUNCTION generate_order_number()
RETURNS TRIGGER AS $$
BEGIN
    NEW.order_number := 'ORD-' || TO_CHAR(now(), 'YYYYMMDD') || '-' || LPAD(nextval('order_number_seq')::TEXT, 6, '0');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Publica eventos no outbox
CREATE OR REPLACE FUNCTION publish_order_event()
RETURNS TRIGGER AS $$
DECLARE
    event_type_name VARCHAR(100);
    event_payload JSONB;
BEGIN
    IF TG_OP = 'INSERT' THEN
        event_type_name := 'OrderCreated';
        event_payload := to_jsonb(NEW);
    ELSIF TG_OP = 'UPDATE' THEN
        IF NEW.status != OLD.status THEN
            event_type_name := 'OrderStatusChanged';
            event_payload := jsonb_build_object(
                'order_id', NEW.id,
                'order_number', NEW.order_number,
                'old_status', OLD.status,
                'new_status', NEW.status,
                'user_id', NEW.user_id
            );
        ELSIF NEW.payment_status != OLD.payment_status THEN
            event_type_name := 'OrderPaymentStatusChanged';
            event_payload := jsonb_build_object(
                'order_id', NEW.id,
                'order_number', NEW.order_number,
                'old_payment_status', OLD.payment_status,
                'new_payment_status', NEW.payment_status,
                'user_id', NEW.user_id
            );
        ELSE
            event_type_name := 'OrderUpdated';
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
        'ORDER',
        event_type_name,
        event_payload,
        jsonb_build_object(
            'user_id', NEW.user_id,
            'order_number', NEW.order_number,
            'correlation_id', uuid_generate_v4()
        )
    );

    RETURN NEW;
END;
$ LANGUAGE plpgsql;

-- Atualiza timestamps baseado em status
CREATE OR REPLACE FUNCTION update_order_timestamps()
RETURNS TRIGGER AS $
BEGIN
    IF NEW.status = 'PAID' AND OLD.status != 'PAID' THEN
        NEW.paid_at := now();
    ELSIF NEW.status = 'SHIPPED' AND OLD.status != 'SHIPPED' THEN
        NEW.shipped_at := now();
    ELSIF NEW.status = 'DELIVERED' AND OLD.status != 'DELIVERED' THEN
        NEW.delivered_at := now();
    ELSIF NEW.status = 'CANCELLED' AND OLD.status != 'CANCELLED' THEN
        NEW.cancelled_at := now();
    END IF;
    
    RETURN NEW;
END;
$ LANGUAGE plpgsql;

-- ========================================
-- TRIGGERS
-- ========================================

-- Updated At
CREATE TRIGGER update_orders_updated_at 
    BEFORE UPDATE ON orders 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_order_payments_updated_at 
    BEFORE UPDATE ON order_payments 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_order_sagas_updated_at 
    BEFORE UPDATE ON order_sagas 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Version Control
CREATE TRIGGER update_orders_version 
    BEFORE UPDATE ON orders 
    FOR EACH ROW EXECUTE FUNCTION increment_version();

-- Order Number Generation
CREATE TRIGGER generate_order_number_trigger 
    BEFORE INSERT ON orders 
    FOR EACH ROW EXECUTE FUNCTION generate_order_number();

-- Timestamps
CREATE TRIGGER update_order_timestamps_trigger 
    BEFORE UPDATE ON orders 
    FOR EACH ROW EXECUTE FUNCTION update_order_timestamps();

-- Event Publishing
CREATE TRIGGER publish_order_created 
    AFTER INSERT ON orders 
    FOR EACH ROW EXECUTE FUNCTION publish_order_event();

CREATE TRIGGER publish_order_updated 
    AFTER UPDATE ON orders 
    FOR EACH ROW EXECUTE FUNCTION publish_order_event();

-- ========================================
-- VIEWS (Read Models / CQRS)
-- ========================================

-- View completa de pedido com itens
CREATE OR REPLACE VIEW order_details AS
SELECT 
    o.id,
    o.order_number,
    o.user_id,
    o.status,
    o.payment_status,
    o.payment_method,
    
    -- Valores
    o.subtotal,
    o.discount_amount,
    o.shipping_amount,
    o.tax_amount,
    o.total,
    
    -- Snapshots
    o.address_snapshot,
    o.card_snapshot,
    o.coupon_snapshot,
    
    -- Itens do pedido
    (
        SELECT jsonb_agg(
            jsonb_build_object(
                'id', oi.id,
                'product_id', oi.product_id,
                'product', oi.product_snapshot,
                'quantity', oi.quantity,
                'unit_price', oi.unit_price,
                'discount_amount', oi.discount_amount,
                'total_price', oi.total_price
            )
        )
        FROM order_items oi
        WHERE oi.order_id = o.id
    ) AS items,
    
    -- Tracking mais recente
    (
        SELECT jsonb_build_object(
            'status', ot.status,
            'tracking_code', ot.tracking_code,
            'carrier', ot.carrier,
            'current_location', ot.current_location,
            'estimated_delivery', ot.estimated_delivery,
            'updated_at', ot.created_at
        )
        FROM order_tracking ot
        WHERE ot.order_id = o.id
        ORDER BY ot.created_at DESC
        LIMIT 1
    ) AS current_tracking,
    
    -- Pagamento
    (
        SELECT jsonb_build_object(
            'id', op.id,
            'status', op.payment_status,
            'amount', op.amount,
            'gateway_provider', op.gateway_provider,
            'card_last4', op.card_last4,
            'card_brand', op.card_brand
        )
        FROM order_payments op
        WHERE op.order_id = o.id
        ORDER BY op.created_at DESC
        LIMIT 1
    ) AS payment_info,
    
    o.created_at,
    o.updated_at,
    o.confirmed_at,
    o.paid_at,
    o.shipped_at,
    o.delivered_at,
    o.cancelled_at
    
FROM orders o;

-- View de pedidos por usuário
CREATE OR REPLACE VIEW user_orders AS
SELECT 
    o.user_id,
    o.id AS order_id,
    o.order_number,
    o.status,
    o.payment_status,
    o.total,
    o.created_at,
    
    (SELECT COUNT(*) FROM order_items WHERE order_id = o.id) AS items_count,
    
    -- Primeira imagem do primeiro produto
    (
        SELECT oi.product_snapshot->>'image_url'
        FROM order_items oi
        WHERE oi.order_id = o.id
        ORDER BY oi.created_at
        LIMIT 1
    ) AS preview_image
    
FROM orders o;

-- ========================================
-- MATERIALIZED VIEWS (para Analytics)
-- ========================================

-- Relatório de vendas diárias
CREATE MATERIALIZED VIEW daily_sales AS
SELECT 
    DATE(created_at) AS sale_date,
    COUNT(*) AS orders_count,
    SUM(subtotal) AS total_subtotal,
    SUM(discount_amount) AS total_discounts,
    SUM(shipping_amount) AS total_shipping,
    SUM(total) AS total_revenue,
    AVG(total) AS avg_order_value,
    
    -- Por status de pagamento
    COUNT(*) FILTER (WHERE payment_status = 'CAPTURED') AS paid_orders,
    COUNT(*) FILTER (WHERE payment_status = 'FAILED') AS failed_orders,
    COUNT(*) FILTER (WHERE status = 'CANCELLED') AS cancelled_orders
    
FROM orders
WHERE created_at >= CURRENT_DATE - INTERVAL '90 days'
GROUP BY DATE(created_at)
ORDER BY sale_date DESC;

CREATE INDEX idx_daily_sales_date ON daily_sales(sale_date);

-- Produtos mais vendidos
CREATE MATERIALIZED VIEW top_selling_products AS
SELECT 
    oi.product_id,
    oi.product_snapshot->>'name' AS product_name,
    oi.product_snapshot->>'sku' AS product_sku,
    COUNT(DISTINCT oi.order_id) AS orders_count,
    SUM(oi.quantity) AS total_quantity_sold,
    SUM(oi.total_price) AS total_revenue,
    AVG(oi.unit_price) AS avg_price
    
FROM order_items oi
JOIN orders o ON o.id = oi.order_id
WHERE o.created_at >= CURRENT_DATE - INTERVAL '30 days'
AND o.status NOT IN ('CANCELLED', 'FAILED')
GROUP BY oi.product_id, oi.product_snapshot->>'name', oi.product_snapshot->>'sku'
ORDER BY total_revenue DESC
LIMIT 100;

-- Refresh materialized views (executar via cron)
-- REFRESH MATERIALIZED VIEW CONCURRENTLY daily_sales;
-- REFRESH MATERIALIZED VIEW CONCURRENTLY top_selling_products;

-- ========================================
-- STORED PROCEDURES (Business Logic)
-- ========================================

-- Cancelar pedido e iniciar saga de compensação
CREATE OR REPLACE FUNCTION cancel_order(
    p_order_id UUID,
    p_reason TEXT,
    p_cancelled_by UUID
) RETURNS JSONB AS $
DECLARE
    v_order orders%ROWTYPE;
    v_saga_id UUID;
BEGIN
    -- Busca pedido
    SELECT * INTO v_order FROM orders WHERE id = p_order_id FOR UPDATE;
    
    IF NOT FOUND THEN
        RETURN jsonb_build_object('success', false, 'error', 'Order not found');
    END IF;
    
    -- Valida se pode cancelar
    IF v_order.status IN ('DELIVERED', 'CANCELLED', 'REFUNDED') THEN
        RETURN jsonb_build_object('success', false, 'error', 'Cannot cancel order in this status');
    END IF;
    
    -- Atualiza pedido
    UPDATE orders
    SET 
        status = 'CANCELLED',
        cancelled_at = now(),
        cancellation_reason = p_reason
    WHERE id = p_order_id;
    
    -- Cria saga de cancelamento
    INSERT INTO order_sagas (
        order_id,
        saga_type,
        status,
        steps
    ) VALUES (
        p_order_id,
        'CANCEL_ORDER',
        'STARTED',
        jsonb_build_array(
            jsonb_build_object('name', 'RefundPayment', 'status', 'PENDING', 'service', 'payment'),
            jsonb_build_object('name', 'RestoreInventory', 'status', 'PENDING', 'service', 'catalog'),
            jsonb_build_object('name', 'SendCancellationEmail', 'status', 'PENDING', 'service', 'notification')
        )
    ) RETURNING id INTO v_saga_id;
    
    RETURN jsonb_build_object(
        'success', true,
        'order_id', p_order_id,
        'saga_id', v_saga_id
    );
END;
$ LANGUAGE plpgsql;

-- Processar pagamento bem-sucedido
CREATE OR REPLACE FUNCTION process_successful_payment(
    p_order_id UUID,
    p_gateway_transaction_id VARCHAR,
    p_gateway_authorization_code VARCHAR
) RETURNS JSONB AS $
BEGIN
    -- Atualiza pedido
    UPDATE orders
    SET 
        status = 'PAID',
        payment_status = 'CAPTURED',
        paid_at = now()
    WHERE id = p_order_id;
    
    -- Atualiza pagamento
    UPDATE order_payments
    SET 
        payment_status = 'CAPTURED',
        gateway_transaction_id = p_gateway_transaction_id,
        gateway_authorization_code = p_gateway_authorization_code,
        captured_at = now()
    WHERE order_id = p_order_id;
    
    RETURN jsonb_build_object('success', true, 'order_id', p_order_id);
END;
$ LANGUAGE plpgsql;

-- ========================================
-- COMMENTS
-- ========================================

COMMENT ON TABLE orders IS 'Tabela principal de pedidos';
COMMENT ON TABLE order_items IS 'Itens do pedido com snapshot de produtos';
COMMENT ON TABLE order_tracking IS 'Histórico de rastreamento do pedido';
COMMENT ON TABLE order_payments IS 'Pagamentos associados ao pedido';
COMMENT ON TABLE order_sagas IS 'Orquestração de sagas para transações distribuídas';
COMMENT ON COLUMN orders.address_snapshot IS 'Snapshot do endereço no momento do pedido (imutável)';
COMMENT ON COLUMN orders.card_snapshot IS 'Snapshot do cartão usado (apenas últimos 4 dígitos)';
COMMENT ON COLUMN orders.coupon_snapshot IS 'Snapshot do cupom aplicado';
COMMENT ON FUNCTION cancel_order IS 'Cancela pedido e inicia saga de compensação';
COMMENT ON MATERIALIZED VIEW daily_sales IS 'Relatório agregado de vendas diárias (atualizar via cron)';