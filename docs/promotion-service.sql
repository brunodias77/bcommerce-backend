-- ========================================
-- PROMOTION SERVICE - DATABASE SCHEMA
-- ========================================
-- Responsável por: Cupons, Descontos, Campanhas Promocionais

-- ========================================
-- EXTENSIONS
-- ========================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ========================================
-- ENUMS
-- ========================================

CREATE TYPE coupon_type AS ENUM ('PERCENTAGE', 'FIXED', 'FREE_SHIPPING', 'BUY_X_GET_Y');
CREATE TYPE discount_target AS ENUM ('CART', 'PRODUCT', 'CATEGORY', 'SHIPPING', 'FIRST_ORDER');
CREATE TYPE coupon_status AS ENUM ('DRAFT', 'ACTIVE', 'PAUSED', 'EXPIRED', 'DEPLETED');

-- ========================================
-- TABLES
-- ========================================

CREATE TABLE coupons (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code VARCHAR(50) UNIQUE NOT NULL,
    name VARCHAR(150) NOT NULL,
    description TEXT,
    
    -- Tipo e valor do desconto
    type coupon_type NOT NULL,
    discount_target discount_target NOT NULL,
    discount_value DECIMAL(10,2) NOT NULL CHECK (discount_value >= 0),
    max_discount_amount DECIMAL(10,2), -- Limite máximo em valor absoluto
    
    -- Regras de aplicação
    min_purchase_amount DECIMAL(10,2) DEFAULT 0 CHECK (min_purchase_amount >= 0),
    target_products JSONB, -- ["product_id_1", "product_id_2"]
    target_categories JSONB, -- ["category_id_1", "category_id_2"]
    excluded_products JSONB,
    excluded_categories JSONB,
    
    -- Buy X Get Y (se type = BUY_X_GET_Y)
    buy_quantity INT,
    get_quantity INT,
    
    -- Validade
    valid_from TIMESTAMP NOT NULL,
    valid_until TIMESTAMP NOT NULL,
    
    -- Limites de uso
    max_uses INT, -- Total de usos permitidos (NULL = ilimitado)
    max_uses_per_user INT DEFAULT 1,
    current_uses INT DEFAULT 0,
    
    -- Restrições
    first_order_only BOOLEAN DEFAULT FALSE,
    min_items_quantity INT DEFAULT 0,
    allowed_payment_methods JSONB, -- ["CREDIT_CARD", "PIX"]
    
    -- Status
    status coupon_status DEFAULT 'DRAFT',
    is_public BOOLEAN DEFAULT TRUE, -- FALSE = cupom secreto/personalizado
    
    -- Stackable (pode ser combinado com outros cupons)
    is_stackable BOOLEAN DEFAULT FALSE,
    
    version INT DEFAULT 1,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now()
);

-- Uso de cupons por usuário
CREATE TABLE user_coupons (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL, -- External reference (Identity Service)
    coupon_id UUID NOT NULL REFERENCES coupons(id) ON DELETE CASCADE,
    
    times_used INT DEFAULT 0,
    total_discount_amount DECIMAL(10,2) DEFAULT 0,
    
    first_used_at TIMESTAMP,
    last_used_at TIMESTAMP,
    
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now(),
    
    UNIQUE(user_id, coupon_id)
);

-- Histórico de uso de cupons (auditoria)
CREATE TABLE coupon_usages (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    coupon_id UUID NOT NULL REFERENCES coupons(id) ON DELETE CASCADE,
    user_id UUID NOT NULL,
    order_id UUID, -- External reference (Order Service)
    
    discount_applied DECIMAL(10,2) NOT NULL CHECK (discount_applied >= 0),
    order_total DECIMAL(10,2),
    
    metadata JSONB DEFAULT '{}', -- {cart_total, items_count, etc}
    
    created_at TIMESTAMP DEFAULT now()
);

-- Campanhas promocionais (agrupa múltiplos cupons)
CREATE TABLE campaigns (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(150) NOT NULL,
    description TEXT,
    slug VARCHAR(180) UNIQUE NOT NULL,
    
    -- Banner/Landing page
    banner_url TEXT,
    landing_page_url TEXT,
    
    -- Período
    starts_at TIMESTAMP NOT NULL,
    ends_at TIMESTAMP NOT NULL,
    
    -- Status
    is_active BOOLEAN DEFAULT TRUE,
    
    -- Analytics
    views_count INT DEFAULT 0,
    conversions_count INT DEFAULT 0,
    total_revenue DECIMAL(12,2) DEFAULT 0,
    
    version INT DEFAULT 1,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now()
);

-- Relacionamento N:N entre campanhas e cupons
CREATE TABLE campaign_coupons (
    campaign_id UUID NOT NULL REFERENCES campaigns(id) ON DELETE CASCADE,
    coupon_id UUID NOT NULL REFERENCES coupons(id) ON DELETE CASCADE,
    display_order INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT now(),
    PRIMARY KEY (campaign_id, coupon_id)
);

-- ========================================
-- OUTBOX PATTERN
-- ========================================

CREATE TYPE outbox_status AS ENUM ('PENDING', 'PROCESSING', 'PUBLISHED', 'FAILED');

CREATE TABLE outbox_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_id UUID NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL, -- COUPON, CAMPAIGN
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

-- Recebe eventos de outros serviços (OrderCompleted, etc)
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

-- Coupons
CREATE INDEX idx_coupons_code ON coupons(code) WHERE status = 'ACTIVE';
CREATE INDEX idx_coupons_status ON coupons(status);
CREATE INDEX idx_coupons_valid_dates ON coupons(valid_from, valid_until) WHERE status = 'ACTIVE';
CREATE INDEX idx_coupons_public ON coupons(is_public) WHERE is_public = TRUE AND status = 'ACTIVE';
CREATE INDEX idx_coupons_target ON coupons(discount_target);

-- User Coupons
CREATE INDEX idx_user_coupons_user_id ON user_coupons(user_id);
CREATE INDEX idx_user_coupons_coupon_id ON user_coupons(coupon_id);
CREATE INDEX idx_user_coupons_last_used ON user_coupons(last_used_at);

-- Coupon Usages
CREATE INDEX idx_coupon_usages_coupon_id ON coupon_usages(coupon_id);
CREATE INDEX idx_coupon_usages_user_id ON coupon_usages(user_id);
CREATE INDEX idx_coupon_usages_order_id ON coupon_usages(order_id) WHERE order_id IS NOT NULL;
CREATE INDEX idx_coupon_usages_created_at ON coupon_usages(created_at);

-- Campaigns
CREATE INDEX idx_campaigns_slug ON campaigns(slug) WHERE is_active = TRUE;
CREATE INDEX idx_campaigns_dates ON campaigns(starts_at, ends_at) WHERE is_active = TRUE;
CREATE INDEX idx_campaigns_active ON campaigns(is_active);

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

-- Atualiza status do cupom baseado em regras
CREATE OR REPLACE FUNCTION update_coupon_status()
RETURNS TRIGGER AS $$
BEGIN
    -- Se atingiu limite de usos
    IF NEW.max_uses IS NOT NULL AND NEW.current_uses >= NEW.max_uses THEN
        NEW.status := 'DEPLETED';
    -- Se passou da data de validade
    ELSIF NEW.valid_until < now() THEN
        NEW.status := 'EXPIRED';
    -- Se ainda não começou
    ELSIF NEW.valid_from > now() THEN
        NEW.status := 'DRAFT';
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Incrementa uso do cupom
CREATE OR REPLACE FUNCTION increment_coupon_usage()
RETURNS TRIGGER AS $$
BEGIN
    -- Atualiza contador global do cupom
    UPDATE coupons
    SET current_uses = current_uses + 1
    WHERE id = NEW.coupon_id;
    
    -- Atualiza ou cria registro de uso por usuário
    INSERT INTO user_coupons (user_id, coupon_id, times_used, total_discount_amount, first_used_at, last_used_at)
    VALUES (NEW.user_id, NEW.coupon_id, 1, NEW.discount_applied, now(), now())
    ON CONFLICT (user_id, coupon_id) DO UPDATE
    SET 
        times_used = user_coupons.times_used + 1,
        total_discount_amount = user_coupons.total_discount_amount + NEW.discount_applied,
        last_used_at = now();
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Publica evento no outbox
CREATE OR REPLACE FUNCTION publish_coupon_event()
RETURNS TRIGGER AS $$
DECLARE
    event_type_name VARCHAR(100);
BEGIN
    IF TG_OP = 'INSERT' THEN
        event_type_name := 'CouponCreated';
    ELSIF TG_OP = 'UPDATE' THEN
        IF NEW.status != OLD.status THEN
            event_type_name := 'CouponStatusChanged';
        ELSE
            event_type_name := 'CouponUpdated';
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
        'COUPON',
        event_type_name,
        to_jsonb(NEW),
        jsonb_build_object('correlation_id', uuid_generate_v4())
    );

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- TRIGGERS
-- ========================================

-- Updated At
CREATE TRIGGER update_coupons_updated_at 
    BEFORE UPDATE ON coupons 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_user_coupons_updated_at 
    BEFORE UPDATE ON user_coupons 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_campaigns_updated_at 
    BEFORE UPDATE ON campaigns 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Version Control
CREATE TRIGGER update_coupons_version 
    BEFORE UPDATE ON coupons 
    FOR EACH ROW EXECUTE FUNCTION increment_version();

CREATE TRIGGER update_campaigns_version 
    BEFORE UPDATE ON campaigns 
    FOR EACH ROW EXECUTE FUNCTION increment_version();

-- Status Management
CREATE TRIGGER update_coupon_status_trigger 
    BEFORE UPDATE ON coupons 
    FOR EACH ROW EXECUTE FUNCTION update_coupon_status();

-- Usage Tracking
CREATE TRIGGER increment_coupon_usage_trigger 
    AFTER INSERT ON coupon_usages 
    FOR EACH ROW EXECUTE FUNCTION increment_coupon_usage();

-- Event Publishing
CREATE TRIGGER publish_coupon_created 
    AFTER INSERT ON coupons 
    FOR EACH ROW EXECUTE FUNCTION publish_coupon_event();

CREATE TRIGGER publish_coupon_updated 
    AFTER UPDATE ON coupons 
    FOR EACH ROW EXECUTE FUNCTION publish_coupon_event();

-- ========================================
-- STORED PROCEDURES
-- ========================================

-- Validar se cupom pode ser usado
CREATE OR REPLACE FUNCTION validate_coupon(
    p_coupon_code VARCHAR,
    p_user_id UUID,
    p_cart_total DECIMAL,
    p_is_first_order BOOLEAN DEFAULT FALSE
) RETURNS JSONB AS $$
DECLARE
    v_coupon coupons%ROWTYPE;
    v_user_usage user_coupons%ROWTYPE;
BEGIN
    -- Busca cupom
    SELECT * INTO v_coupon 
    FROM coupons 
    WHERE code = p_coupon_code 
    AND status = 'ACTIVE'
    FOR UPDATE;
    
    IF NOT FOUND THEN
        RETURN jsonb_build_object('valid', false, 'error', 'Coupon not found or inactive');
    END IF;
    
    -- Valida data
    IF now() < v_coupon.valid_from OR now() > v_coupon.valid_until THEN
        RETURN jsonb_build_object('valid', false, 'error', 'Coupon expired or not yet valid');
    END IF;
    
    -- Valida limite global de usos
    IF v_coupon.max_uses IS NOT NULL AND v_coupon.current_uses >= v_coupon.max_uses THEN
        RETURN jsonb_build_object('valid', false, 'error', 'Coupon usage limit reached');
    END IF;
    
    -- Valida valor mínimo de compra
    IF p_cart_total < v_coupon.min_purchase_amount THEN
        RETURN jsonb_build_object(
            'valid', false, 
            'error', format('Minimum purchase amount of %s required', v_coupon.min_purchase_amount)
        );
    END IF;
    
    -- Valida primeira compra
    IF v_coupon.first_order_only AND NOT p_is_first_order THEN
        RETURN jsonb_build_object('valid', false, 'error', 'Coupon valid only for first order');
    END IF;
    
    -- Valida limite por usuário
    SELECT * INTO v_user_usage 
    FROM user_coupons 
    WHERE user_id = p_user_id AND coupon_id = v_coupon.id;
    
    IF FOUND AND v_user_usage.times_used >= v_coupon.max_uses_per_user THEN
        RETURN jsonb_build_object('valid', false, 'error', 'User usage limit reached for this coupon');
    END IF;
    
    -- Cupom válido
    RETURN jsonb_build_object(
        'valid', true,
        'coupon', to_jsonb(v_coupon)
    );
END;
$$ LANGUAGE plpgsql;

-- Calcular desconto
CREATE OR REPLACE FUNCTION calculate_discount(
    p_coupon_id UUID,
    p_cart_total DECIMAL,
    p_shipping_amount DECIMAL DEFAULT 0
) RETURNS DECIMAL AS $$
DECLARE
    v_coupon coupons%ROWTYPE;
    v_discount DECIMAL;
BEGIN
    SELECT * INTO v_coupon FROM coupons WHERE id = p_coupon_id;
    
    IF NOT FOUND THEN
        RETURN 0;
    END IF;
    
    CASE v_coupon.type
        WHEN 'PERCENTAGE' THEN
            v_discount := (p_cart_total * v_coupon.discount_value / 100);
            
            -- Aplica limite máximo se configurado
            IF v_coupon.max_discount_amount IS NOT NULL THEN
                v_discount := LEAST(v_discount, v_coupon.max_discount_amount);
            END IF;
            
        WHEN 'FIXED' THEN
            v_discount := v_coupon.discount_value;
            
        WHEN 'FREE_SHIPPING' THEN
            v_discount := p_shipping_amount;
            
        ELSE
            v_discount := 0;
    END CASE;
    
    -- Garante que desconto não excede total
    v_discount := LEAST(v_discount, p_cart_total);
    
    RETURN v_discount;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- VIEWS
-- ========================================

-- Cupons ativos e públicos
CREATE OR REPLACE VIEW active_public_coupons AS
SELECT 
    c.id,
    c.code,
    c.name,
    c.description,
    c.type,
    c.discount_target,
    c.discount_value,
    c.max_discount_amount,
    c.min_purchase_amount,
    c.valid_from,
    c.valid_until,
    c.max_uses,
    c.current_uses,
    c.max_uses_per_user,
    c.first_order_only,
    
    -- Porcentagem de uso
    CASE 
        WHEN c.max_uses IS NOT NULL THEN 
            ROUND((c.current_uses::DECIMAL / c.max_uses::DECIMAL) * 100, 2)
        ELSE NULL
    END AS usage_percentage,
    
    -- Disponibilidade
    CASE
        WHEN c.max_uses IS NOT NULL AND c.current_uses >= c.max_uses THEN FALSE
        WHEN c.valid_until < now() THEN FALSE
        ELSE TRUE
    END AS is_available
    
FROM coupons c
WHERE c.status = 'ACTIVE'
AND c.is_public = TRUE
AND c.valid_from <= now()
AND c.valid_until >= now();

-- Estatísticas de campanha
CREATE OR REPLACE VIEW campaign_statistics AS
SELECT 
    camp.id,
    camp.name,
    camp.slug,
    camp.starts_at,
    camp.ends_at,
    camp.is_active,
    
    -- Contadores
    camp.views_count,
    camp.conversions_count,
    camp.total_revenue,
    
    -- Taxa de conversão
    CASE 
        WHEN camp.views_count > 0 THEN
            ROUND((camp.conversions_count::DECIMAL / camp.views_count::DECIMAL) * 100, 2)
        ELSE 0
    END AS conversion_rate,
    
    -- Cupons associados
    (
        SELECT COUNT(*) 
        FROM campaign_coupons cc 
        WHERE cc.campaign_id = camp.id
    ) AS coupons_count,
    
    -- Uso total dos cupons da campanha
    (
        SELECT COALESCE(SUM(c.current_uses), 0)
        FROM campaign_coupons cc
        JOIN coupons c ON c.id = cc.coupon_id
        WHERE cc.campaign_id = camp.id
    ) AS total_coupon_uses
    
FROM campaigns camp;

-- ========================================
-- MATERIALIZED VIEWS (Analytics)
-- ========================================

-- Top cupons por uso
CREATE MATERIALIZED VIEW top_coupons_by_usage AS
SELECT 
    c.id,
    c.code,
    c.name,
    c.type,
    c.discount_target,
    c.current_uses,
    
    -- Estatísticas de uso
    COUNT(DISTINCT cu.user_id) AS unique_users,
    COALESCE(SUM(cu.discount_applied), 0) AS total_discount_given,
    COALESCE(AVG(cu.discount_applied), 0) AS avg_discount_per_use,
    
    -- Última utilização
    MAX(cu.created_at) AS last_used_at
    
FROM coupons c
LEFT JOIN coupon_usages cu ON cu.coupon_id = c.id
WHERE c.created_at >= CURRENT_DATE - INTERVAL '90 days'
GROUP BY c.id, c.code, c.name, c.type, c.discount_target, c.current_uses
ORDER BY c.current_uses DESC
LIMIT 50;

-- Refresh via cron: REFRESH MATERIALIZED VIEW CONCURRENTLY top_coupons_by_usage;

-- ========================================
-- COMMENTS
-- ========================================

COMMENT ON TABLE coupons IS 'Cupons e códigos promocionais';
COMMENT ON TABLE user_coupons IS 'Rastreamento de uso de cupons por usuário';
COMMENT ON TABLE coupon_usages IS 'Histórico completo de uso de cupons (auditoria)';
COMMENT ON TABLE campaigns IS 'Campanhas promocionais que agrupam múltiplos cupons';
COMMENT ON COLUMN coupons.is_stackable IS 'Se TRUE, pode ser combinado com outros cupons';
COMMENT ON COLUMN coupons.is_public IS 'Se FALSE, cupom é secreto/personalizado';
COMMENT ON FUNCTION validate_coupon IS 'Valida se um cupom pode ser usado por um usuário';
COMMENT ON FUNCTION calculate_discount IS 'Calcula o valor do desconto baseado no tipo de cupom';