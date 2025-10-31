-- ========================================
-- NOTIFICATION SERVICE - DATABASE SCHEMA
-- ========================================
-- Responsável por: Emails, SMS, Push Notifications, Templates

-- ========================================
-- EXTENSIONS
-- ========================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ========================================
-- ENUMS
-- ========================================

CREATE TYPE notification_type AS ENUM (
    'EMAIL',
    'SMS',
    'PUSH',
    'IN_APP',
    'WEBHOOK'
);

CREATE TYPE notification_status AS ENUM (
    'PENDING',
    'QUEUED',
    'SENDING',
    'SENT',
    'DELIVERED',
    'OPENED',
    'CLICKED',
    'FAILED',
    'BOUNCED',
    'SPAM',
    'UNSUBSCRIBED'
);

CREATE TYPE notification_priority AS ENUM (
    'LOW',
    'NORMAL',
    'HIGH',
    'URGENT'
);

CREATE TYPE notification_category AS ENUM (
    'TRANSACTIONAL',    -- Order confirmations, receipts
    'MARKETING',        -- Promotional emails
    'SYSTEM',          -- Password resets, verifications
    'OPERATIONAL'      -- System alerts, admin notifications
);

-- ========================================
-- TABLES
-- ========================================

CREATE TABLE notifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL, -- External reference (Identity Service)
    
    -- Tipo e categoria
    type notification_type NOT NULL,
    category notification_category NOT NULL,
    priority notification_priority DEFAULT 'NORMAL',
    
    -- Status
    status notification_status DEFAULT 'PENDING',
    
    -- Conteúdo
    subject VARCHAR(255),
    body TEXT NOT NULL,
    html_body TEXT,
    
    -- Destinatário
    recipient_email VARCHAR(255),
    recipient_phone VARCHAR(20),
    recipient_device_token TEXT,
    recipient_webhook_url TEXT,
    
    -- Template utilizado
    template_id UUID,
    template_variables JSONB,
    
    -- Envio
    provider VARCHAR(50), -- SENDGRID, TWILIO, FCM, etc
    provider_message_id VARCHAR(255),
    provider_response JSONB,
    
    -- Agendamento
    scheduled_for TIMESTAMP,
    sent_at TIMESTAMP,
    delivered_at TIMESTAMP,
    opened_at TIMESTAMP,
    clicked_at TIMESTAMP,
    failed_at TIMESTAMP,
    
    -- Retry logic
    retry_count INT DEFAULT 0,
    max_retries INT DEFAULT 3,
    next_retry_at TIMESTAMP,
    error_message TEXT,
    error_code VARCHAR(50),
    
    -- Tracking
    tracking_id VARCHAR(100),
    opens_count INT DEFAULT 0,
    clicks_count INT DEFAULT 0,
    
    -- Metadata
    metadata JSONB DEFAULT '{}',
    -- {
    --   "order_id": "...",
    --   "correlation_id": "...",
    --   "user_agent": "...",
    --   "ip_address": "..."
    -- }
    
    -- Timestamps
    version INT DEFAULT 1,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now(),
    expires_at TIMESTAMP -- Para limpeza automática
);

CREATE TABLE notification_templates (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(150) UNIQUE NOT NULL,
    slug VARCHAR(180) UNIQUE NOT NULL,
    description TEXT,
    
    type notification_type NOT NULL,
    category notification_category NOT NULL,
    
    -- Conteúdo do template
    subject_template VARCHAR(255),
    body_template TEXT NOT NULL,
    html_template TEXT,
    
    -- Variáveis disponíveis
    available_variables JSONB,
    -- {
    --   "user_name": "string",
    --   "order_number": "string",
    --   "total_amount": "number",
    --   "order_items": "array"
    -- }
    
    -- Configurações
    from_email VARCHAR(255),
    from_name VARCHAR(150),
    reply_to_email VARCHAR(255),
    cc_emails JSONB,
    bcc_emails JSONB,
    
    -- Status
    is_active BOOLEAN DEFAULT TRUE,
    
    -- A/B Testing
    ab_test_variant VARCHAR(50),
    
    version INT DEFAULT 1,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now()
);

-- Preferências de notificação do usuário
CREATE TABLE notification_preferences (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL UNIQUE, -- External reference
    
    -- Opt-ins por canal
    email_enabled BOOLEAN DEFAULT TRUE,
    sms_enabled BOOLEAN DEFAULT FALSE,
    push_enabled BOOLEAN DEFAULT TRUE,
    
    -- Opt-ins por categoria
    transactional_enabled BOOLEAN DEFAULT TRUE,
    marketing_enabled BOOLEAN DEFAULT TRUE,
    system_enabled BOOLEAN DEFAULT TRUE,
    operational_enabled BOOLEAN DEFAULT FALSE,
    
    -- Frequência
    marketing_frequency VARCHAR(50) DEFAULT 'DAILY', -- REALTIME, DAILY, WEEKLY
    digest_enabled BOOLEAN DEFAULT FALSE,
    digest_time TIME DEFAULT '09:00:00',
    
    -- Timezone
    timezone VARCHAR(50) DEFAULT 'America/Sao_Paulo',
    
    version INT DEFAULT 1,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now()
);

-- Eventos de notificação (tracking)
CREATE TABLE notification_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    notification_id UUID NOT NULL REFERENCES notifications(id) ON DELETE CASCADE,
    
    event_type VARCHAR(50) NOT NULL, -- SENT, DELIVERED, OPENED, CLICKED, BOUNCED, etc
    event_data JSONB DEFAULT '{}',
    
    -- Tracking de cliques
    link_url TEXT,
    
    -- Device/Browser info
    user_agent TEXT,
    ip_address INET,
    
    created_at TIMESTAMP DEFAULT now()
);

-- Webhooks configurados
CREATE TABLE notification_webhooks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(150) NOT NULL,
    url TEXT NOT NULL,
    
    -- Eventos que disparam o webhook
    event_types JSONB NOT NULL, -- ["notification.sent", "notification.failed"]
    
    -- Autenticação
    auth_type VARCHAR(50), -- BEARER, BASIC, HMAC
    auth_credentials JSONB, -- Encrypted
    
    -- Headers customizados
    custom_headers JSONB,
    
    -- Status
    is_active BOOLEAN DEFAULT TRUE,
    
    -- Retry config
    retry_count INT DEFAULT 0,
    max_retries INT DEFAULT 3,
    
    -- Stats
    success_count INT DEFAULT 0,
    failure_count INT DEFAULT 0,
    last_triggered_at TIMESTAMP,
    last_success_at TIMESTAMP,
    last_failure_at TIMESTAMP,
    
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now()
);

-- ========================================
-- OUTBOX PATTERN
-- ========================================

CREATE TYPE outbox_status AS ENUM ('PENDING', 'PROCESSING', 'PUBLISHED', 'FAILED');

CREATE TABLE outbox_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_id UUID NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL, -- NOTIFICATION
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

-- Recebe eventos de outros serviços
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

-- Notifications
CREATE INDEX idx_notifications_user_id ON notifications(user_id);
CREATE INDEX idx_notifications_status ON notifications(status);
CREATE INDEX idx_notifications_type ON notifications(type);
CREATE INDEX idx_notifications_category ON notifications(category);
CREATE INDEX idx_notifications_priority ON notifications(priority);
CREATE INDEX idx_notifications_scheduled ON notifications(scheduled_for) WHERE status = 'PENDING';
CREATE INDEX idx_notifications_retry ON notifications(next_retry_at) WHERE status = 'FAILED' AND retry_count < max_retries;
CREATE INDEX idx_notifications_created_at ON notifications(created_at);
CREATE INDEX idx_notifications_expires_at ON notifications(expires_at) WHERE expires_at IS NOT NULL;
CREATE INDEX idx_notifications_tracking ON notifications(tracking_id) WHERE tracking_id IS NOT NULL;

-- Templates
CREATE INDEX idx_notification_templates_slug ON notification_templates(slug) WHERE is_active = TRUE;
CREATE INDEX idx_notification_templates_type ON notification_templates(type) WHERE is_active = TRUE;
CREATE INDEX idx_notification_templates_category ON notification_templates(category) WHERE is_active = TRUE;

-- Preferences
CREATE INDEX idx_notification_preferences_user_id ON notification_preferences(user_id);

-- Events
CREATE INDEX idx_notification_events_notification_id ON notification_events(notification_id);
CREATE INDEX idx_notification_events_type ON notification_events(event_type);
CREATE INDEX idx_notification_events_created_at ON notification_events(created_at);

-- Webhooks
CREATE INDEX idx_notification_webhooks_active ON notification_webhooks(is_active) WHERE is_active = TRUE;

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

-- Atualiza status baseado em eventos
CREATE OR REPLACE FUNCTION update_notification_from_event()
RETURNS TRIGGER AS $$
BEGIN
    CASE NEW.event_type
        WHEN 'SENT' THEN
            UPDATE notifications
            SET 
                status = 'SENT',
                sent_at = NEW.created_at
            WHERE id = NEW.notification_id;
            
        WHEN 'DELIVERED' THEN
            UPDATE notifications
            SET 
                status = 'DELIVERED',
                delivered_at = NEW.created_at
            WHERE id = NEW.notification_id;
            
        WHEN 'OPENED' THEN
            UPDATE notifications
            SET 
                status = 'OPENED',
                opened_at = COALESCE(opened_at, NEW.created_at),
                opens_count = opens_count + 1
            WHERE id = NEW.notification_id;
            
        WHEN 'CLICKED' THEN
            UPDATE notifications
            SET 
                status = 'CLICKED',
                clicked_at = COALESCE(clicked_at, NEW.created_at),
                clicks_count = clicks_count + 1
            WHERE id = NEW.notification_id;
            
        WHEN 'FAILED', 'BOUNCED', 'SPAM' THEN
            UPDATE notifications
            SET 
                status = NEW.event_type::notification_status,
                failed_at = NEW.created_at,
                error_message = NEW.event_data->>'error_message'
            WHERE id = NEW.notification_id;
    END CASE;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Renderiza template com variáveis
CREATE OR REPLACE FUNCTION render_template(
    p_template_id UUID,
    p_variables JSONB
) RETURNS JSONB AS $$
DECLARE
    v_template notification_templates%ROWTYPE;
    v_subject TEXT;
    v_body TEXT;
    v_html TEXT;
    v_key TEXT;
    v_value TEXT;
BEGIN
    SELECT * INTO v_template FROM notification_templates WHERE id = p_template_id AND is_active = TRUE;
    
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Template not found or inactive';
    END IF;
    
    v_subject := v_template.subject_template;
    v_body := v_template.body_template;
    v_html := v_template.html_template;
    
    -- Substitui variáveis no formato {{variable_name}}
    FOR v_key, v_value IN SELECT * FROM jsonb_each_text(p_variables)
    LOOP
        v_subject := REPLACE(v_subject, '{{' || v_key || '}}', v_value);
        v_body := REPLACE(v_body, '{{' || v_key || '}}', v_value);
        IF v_html IS NOT NULL THEN
            v_html := REPLACE(v_html, '{{' || v_key || '}}', v_value);
        END IF;
    END LOOP;
    
    RETURN jsonb_build_object(
        'subject', v_subject,
        'body', v_body,
        'html_body', v_html,
        'from_email', v_template.from_email,
        'from_name', v_template.from_name
    );
END;
$$ LANGUAGE plpgsql;

-- Verifica se usuário aceita receber notificação
CREATE OR REPLACE FUNCTION can_send_notification(
    p_user_id UUID,
    p_type notification_type,
    p_category notification_category
) RETURNS BOOLEAN AS $$
DECLARE
    v_prefs notification_preferences%ROWTYPE;
BEGIN
    SELECT * INTO v_prefs FROM notification_preferences WHERE user_id = p_user_id;
    
    -- Se não tem preferências, assume defaults
    IF NOT FOUND THEN
        -- Transactional sempre pode enviar
        IF p_category = 'TRANSACTIONAL' THEN
            RETURN TRUE;
        END IF;
        RETURN TRUE; -- Default allow
    END IF;
    
    -- Verifica por canal
    CASE p_type
        WHEN 'EMAIL' THEN
            IF NOT v_prefs.email_enabled THEN RETURN FALSE; END IF;
        WHEN 'SMS' THEN
            IF NOT v_prefs.sms_enabled THEN RETURN FALSE; END IF;
        WHEN 'PUSH' THEN
            IF NOT v_prefs.push_enabled THEN RETURN FALSE; END IF;
    END CASE;
    
    -- Verifica por categoria
    CASE p_category
        WHEN 'TRANSACTIONAL' THEN
            IF NOT v_prefs.transactional_enabled THEN RETURN FALSE; END IF;
        WHEN 'MARKETING' THEN
            IF NOT v_prefs.marketing_enabled THEN RETURN FALSE; END IF;
        WHEN 'SYSTEM' THEN
            IF NOT v_prefs.system_enabled THEN RETURN FALSE; END IF;
        WHEN 'OPERATIONAL' THEN
            IF NOT v_prefs.operational_enabled THEN RETURN FALSE; END IF;
    END CASE;
    
    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;

-- Publica evento no outbox
CREATE OR REPLACE FUNCTION publish_notification_event()
RETURNS TRIGGER AS $$
DECLARE
    event_type_name VARCHAR(100);
BEGIN
    IF TG_OP = 'INSERT' THEN
        event_type_name := 'NotificationCreated';
    ELSIF TG_OP = 'UPDATE' THEN
        IF NEW.status != OLD.status THEN
            event_type_name := 'NotificationStatusChanged';
        ELSE
            RETURN NEW;
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
        'NOTIFICATION',
        event_type_name,
        jsonb_build_object(
            'notification_id', NEW.id,
            'user_id', NEW.user_id,
            'type', NEW.type,
            'status', NEW.status,
            'category', NEW.category
        ),
        jsonb_build_object(
            'user_id', NEW.user_id,
            'correlation_id', uuid_generate_v4()
        )
    );

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- TRIGGERS
-- ========================================

-- Updated At
CREATE TRIGGER update_notifications_updated_at 
    BEFORE UPDATE ON notifications 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_notification_templates_updated_at 
    BEFORE UPDATE ON notification_templates 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_notification_preferences_updated_at 
    BEFORE UPDATE ON notification_preferences 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Version Control
CREATE TRIGGER update_notifications_version 
    BEFORE UPDATE ON notifications 
    FOR EACH ROW EXECUTE FUNCTION increment_version();

CREATE TRIGGER update_notification_templates_version 
    BEFORE UPDATE ON notification_templates 
    FOR EACH ROW EXECUTE FUNCTION increment_version();

-- Event Processing
CREATE TRIGGER process_notification_event 
    AFTER INSERT ON notification_events 
    FOR EACH ROW EXECUTE FUNCTION update_notification_from_event();

-- Outbox Publishing
CREATE TRIGGER publish_notification_status_change 
    AFTER UPDATE ON notifications 
    FOR EACH ROW EXECUTE FUNCTION publish_notification_event();

-- ========================================
-- STORED PROCEDURES
-- ========================================

-- Criar notificação a partir de template
CREATE OR REPLACE FUNCTION create_notification_from_template(
    p_template_slug VARCHAR,
    p_user_id UUID,
    p_variables JSONB,
    p_recipient_email VARCHAR DEFAULT NULL,
    p_scheduled_for TIMESTAMP DEFAULT NULL
) RETURNS UUID AS $$
DECLARE
    v_template notification_templates%ROWTYPE;
    v_rendered JSONB;
    v_notification_id UUID;
    v_can_send BOOLEAN;
BEGIN
    -- Busca template
    SELECT * INTO v_template FROM notification_templates 
    WHERE slug = p_template_slug AND is_active = TRUE;
    
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Template % not found', p_template_slug;
    END IF;
    
    -- Verifica se pode enviar
    v_can_send := can_send_notification(p_user_id, v_template.type, v_template.category);
    
    IF NOT v_can_send THEN
        RAISE EXCEPTION 'User opted out of this notification type/category';
    END IF;
    
    -- Renderiza template
    v_rendered := render_template(v_template.id, p_variables);
    
    -- Cria notificação
    INSERT INTO notifications (
        user_id,
        type,
        category,
        priority,
        subject,
        body,
        html_body,
        recipient_email,
        template_id,
        template_variables,
        scheduled_for,
        status
    ) VALUES (
        p_user_id,
        v_template.type,
        v_template.category,
        'NORMAL',
        v_rendered->>'subject',
        v_rendered->>'body',
        v_rendered->>'html_body',
        COALESCE(p_recipient_email, v_rendered->>'from_email'),
        v_template.id,
        p_variables,
        p_scheduled_for,
        CASE WHEN p_scheduled_for IS NULL THEN 'PENDING' ELSE 'QUEUED' END
    ) RETURNING id INTO v_notification_id;
    
    RETURN v_notification_id;
END;
$$ LANGUAGE plpgsql;

-- Processar fila de notificações pendentes
CREATE OR REPLACE FUNCTION process_pending_notifications(
    p_batch_size INT DEFAULT 100
) RETURNS INT AS $$
DECLARE
    v_processed INT := 0;
BEGIN
    UPDATE notifications
    SET status = 'QUEUED'
    WHERE id IN (
        SELECT id 
        FROM notifications
        WHERE status = 'PENDING'
        AND (scheduled_for IS NULL OR scheduled_for <= now())
        ORDER BY priority DESC, created_at
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    );
    
    GET DIAGNOSTICS v_processed = ROW_COUNT;
    RETURN v_processed;
END;
$$ LANGUAGE plpgsql;

-- Limpar notificações antigas
CREATE OR REPLACE FUNCTION cleanup_old_notifications() RETURNS INT AS $$
DECLARE
    v_deleted INT;
BEGIN
    DELETE FROM notifications
    WHERE (
        expires_at IS NOT NULL AND expires_at < now()
    ) OR (
        created_at < now() - INTERVAL '90 days'
        AND status IN ('SENT', 'DELIVERED', 'OPENED', 'CLICKED')
        AND category != 'TRANSACTIONAL'
    );
    
    GET DIAGNOSTICS v_deleted = ROW_COUNT;
    RETURN v_deleted;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- VIEWS
-- ========================================

-- Notificações pendentes de envio
CREATE OR REPLACE VIEW pending_notifications AS
SELECT 
    n.id,
    n.user_id,
    n.type,
    n.category,
    n.priority,
    n.subject,
    n.recipient_email,
    n.recipient_phone,
    n.scheduled_for,
    n.retry_count,
    n.created_at
FROM notifications n
WHERE n.status IN ('PENDING', 'QUEUED')
AND (n.scheduled_for IS NULL OR n.scheduled_for <= now())
ORDER BY n.priority DESC, n.created_at;

-- Estatísticas por template
CREATE OR REPLACE VIEW template_statistics AS
SELECT 
    nt.id,
    nt.name,
    nt.slug,
    nt.type,
    nt.category,
    
    COUNT(n.id) AS total_sent,
    COUNT(n.id) FILTER (WHERE n.status = 'DELIVERED') AS delivered_count,
    COUNT(n.id) FILTER (WHERE n.status = 'OPENED') AS opened_count,
    COUNT(n.id) FILTER (WHERE n.status = 'CLICKED') AS clicked_count,
    COUNT(n.id) FILTER (WHERE n.status = 'FAILED') AS failed_count,
    
    -- Taxas
    CASE 
        WHEN COUNT(n.id) > 0 THEN
            ROUND((COUNT(n.id) FILTER (WHERE n.status = 'DELIVERED')::DECIMAL / COUNT(n.id)::DECIMAL) * 100, 2)
        ELSE 0
    END AS delivery_rate,
    
    CASE 
        WHEN COUNT(n.id) FILTER (WHERE n.status = 'DELIVERED') > 0 THEN
            ROUND((COUNT(n.id) FILTER (WHERE n.status = 'OPENED')::DECIMAL / COUNT(n.id) FILTER (WHERE n.status = 'DELIVERED')::DECIMAL) * 100, 2)
        ELSE 0
    END AS open_rate,
    
    CASE 
        WHEN COUNT(n.id) FILTER (WHERE n.status = 'OPENED') > 0 THEN
            ROUND((COUNT(n.id) FILTER (WHERE n.status = 'CLICKED')::DECIMAL / COUNT(n.id) FILTER (WHERE n.status = 'OPENED')::DECIMAL) * 100, 2)
        ELSE 0
    END AS click_rate
    
FROM notification_templates nt
LEFT JOIN notifications n ON n.template_id = nt.id
WHERE nt.is_active = TRUE
GROUP BY nt.id, nt.name, nt.slug, nt.type, nt.category;

-- ========================================
-- SCHEDULED JOBS (via pg_cron ou cron)
-- ========================================

-- Processar notificações pendentes (a cada minuto)
-- SELECT process_pending_notifications(100);

-- Limpar notificações antigas (diariamente)
-- SELECT cleanup_old_notifications();

-- Processar retries de notificações falhadas
-- UPDATE notifications SET status = 'PENDING', retry_count = retry_count + 1
-- WHERE status = 'FAILED' AND retry_count < max_retries AND next_retry_at <= now();

-- ========================================
-- COMMENTS
-- ========================================

COMMENT ON TABLE notifications IS 'Notificações enviadas aos usuários (email, SMS, push, etc)';
COMMENT ON TABLE notification_templates IS 'Templates reutilizáveis para notificações';
COMMENT ON TABLE notification_preferences IS 'Preferências de notificação por usuário';
COMMENT ON TABLE notification_events IS 'Eventos de rastreamento (opens, clicks, bounces)';
COMMENT ON FUNCTION render_template IS 'Renderiza template substituindo variáveis no formato {{variable}}';
COMMENT ON FUNCTION can_send_notification IS 'Verifica se usuário aceita receber a notificação';
COMMENT ON FUNCTION create_notification_from_template IS 'Cria e agenda notificação a partir de template';
COMMENT ON FUNCTION process_pending_notifications IS 'Processa batch de notificações pendentes (executar via cron)';
COMMENT ON FUNCTION cleanup_old_notifications IS 'Remove notificações antigas para liberar espaço';