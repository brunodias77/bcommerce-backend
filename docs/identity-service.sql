-- ========================================
-- IDENTITY SERVICE - DATABASE SCHEMA (CORRIGIDO)
-- ========================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ========================================
-- TABLES
-- ========================================

CREATE TABLE users (
    user_id VARCHAR(450) PRIMARY KEY,           -- PK compatível com ASP.NET Identity
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    birth_date DATE,
    is_active BOOLEAN DEFAULT true,
    is_verified BOOLEAN DEFAULT false,
    last_login TIMESTAMP,
    metadata JSONB DEFAULT '{}',
    version INTEGER DEFAULT 1,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now(),
    deleted_at TIMESTAMP,
    
    -- Campos do ASP.NET Identity (obrigatórios)
    username VARCHAR(256),
    normalized_username VARCHAR(256),
    email VARCHAR(256),
    normalized_email VARCHAR(256),
    email_confirmed BOOLEAN DEFAULT false,
    password_hash TEXT,
    security_stamp TEXT,
    concurrency_stamp TEXT,
    phone_number TEXT,
    phone_number_confirmed BOOLEAN DEFAULT false,
    two_factor_enabled BOOLEAN DEFAULT false,
    lockout_end TIMESTAMPTZ,
    lockout_enabled BOOLEAN DEFAULT true,
    access_failed_count INTEGER DEFAULT 0
);

-- CORREÇÃO: user_id agora é VARCHAR(450) e referencia users(user_id)
CREATE TABLE refresh_tokens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id VARCHAR(450) NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    token TEXT NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    revoked BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now()
);

CREATE TABLE security_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id VARCHAR(450) REFERENCES users(user_id) ON DELETE SET NULL,
    action VARCHAR(100) NOT NULL,
    ip_address INET,
    user_agent TEXT,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP DEFAULT now()
);

CREATE TABLE addresses (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id VARCHAR(450) NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    label VARCHAR(50),
    street VARCHAR(255) NOT NULL,
    number VARCHAR(20),
    complement VARCHAR(100),
    neighborhood VARCHAR(100),
    city VARCHAR(100) NOT NULL,
    state VARCHAR(100) NOT NULL,
    postal_code VARCHAR(20) NOT NULL,
    country VARCHAR(100) NOT NULL DEFAULT 'BR',
    is_default BOOLEAN DEFAULT FALSE,
    version INT DEFAULT 1,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now(),
    deleted_at TIMESTAMP
);

CREATE TABLE cards (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id VARCHAR(450) NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    cardholder_name VARCHAR(100) NOT NULL,
    card_last4 VARCHAR(4) NOT NULL,
    card_brand VARCHAR(50) NOT NULL,
    expiration_month INT NOT NULL CHECK (expiration_month >= 1 AND expiration_month <= 12),
    expiration_year INT NOT NULL CHECK (expiration_year >= 2024),
    is_default BOOLEAN DEFAULT FALSE,
    token_gateway TEXT,
    version INT DEFAULT 1,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now(),
    deleted_at TIMESTAMP
);

-- ========================================
-- OUTBOX & INBOX PATTERN
-- ========================================

CREATE TYPE outbox_status AS ENUM ('PENDING', 'PROCESSING', 'PUBLISHED', 'FAILED');

CREATE TABLE outbox_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_id VARCHAR(450) NOT NULL,  -- CORRIGIDO: agora suporta VARCHAR
    aggregate_type VARCHAR(100) NOT NULL,
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

CREATE TABLE inbox_events (
    id UUID PRIMARY KEY,
    event_type VARCHAR(100) NOT NULL,
    aggregate_id VARCHAR(450) NOT NULL,  -- CORRIGIDO
    processed_at TIMESTAMP DEFAULT now(),
    created_at TIMESTAMP DEFAULT now()
);

-- ========================================
-- ASP.NET IDENTITY TABLES
-- ========================================

CREATE TABLE AspNetRoles (
    Id TEXT PRIMARY KEY,
    Name VARCHAR(256),
    NormalizedName VARCHAR(256),
    ConcurrencyStamp TEXT
);

CREATE TABLE AspNetRoleClaims (
    Id SERIAL PRIMARY KEY,
    RoleId TEXT REFERENCES AspNetRoles(Id) ON DELETE CASCADE,
    ClaimType TEXT,
    ClaimValue TEXT
);

CREATE TABLE AspNetUserClaims (
    Id SERIAL PRIMARY KEY,
    UserId VARCHAR(450) REFERENCES users(user_id) ON DELETE CASCADE,
    ClaimType TEXT,
    ClaimValue TEXT
);

CREATE TABLE AspNetUserLogins (
    LoginProvider TEXT,
    ProviderKey TEXT,
    ProviderDisplayName TEXT,
    UserId VARCHAR(450) REFERENCES users(user_id) ON DELETE CASCADE,
    PRIMARY KEY (LoginProvider, ProviderKey)
);

CREATE TABLE AspNetUserRoles (
    UserId VARCHAR(450) REFERENCES users(user_id) ON DELETE CASCADE,
    RoleId TEXT REFERENCES AspNetRoles(Id) ON DELETE CASCADE,
    PRIMARY KEY (UserId, RoleId)
);

CREATE TABLE AspNetUserTokens (
    UserId VARCHAR(450) REFERENCES users(user_id) ON DELETE CASCADE,
    LoginProvider TEXT,
    Name TEXT,
    Value TEXT,
    PRIMARY KEY (UserId, LoginProvider, Name)
);

-- ========================================
-- INDEXES
-- ========================================

CREATE INDEX idx_users_email ON users(normalized_email) WHERE deleted_at IS NULL;
CREATE INDEX idx_users_username ON users(normalized_username) WHERE deleted_at IS NULL;
CREATE INDEX idx_users_active ON users(is_active) WHERE deleted_at IS NULL;

CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX idx_refresh_tokens_token ON refresh_tokens(token) WHERE revoked = FALSE;
CREATE INDEX idx_refresh_tokens_expires_at ON refresh_tokens(expires_at);

CREATE INDEX idx_security_logs_user_id ON security_logs(user_id);
CREATE INDEX idx_security_logs_created_at ON security_logs(created_at);
CREATE INDEX idx_security_logs_action ON security_logs(action);

CREATE INDEX idx_addresses_user_id ON addresses(user_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_addresses_default ON addresses(user_id, is_default) WHERE deleted_at IS NULL AND is_default = TRUE;

CREATE INDEX idx_cards_user_id ON cards(user_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_cards_default ON cards(user_id, is_default) WHERE deleted_at IS NULL AND is_default = TRUE;

CREATE INDEX idx_outbox_events_status ON outbox_events(status) WHERE status IN ('PENDING', 'FAILED');
CREATE INDEX idx_outbox_events_created_at ON outbox_events(created_at);
CREATE INDEX idx_outbox_events_aggregate ON outbox_events(aggregate_id, aggregate_type);

CREATE INDEX idx_inbox_events_aggregate ON inbox_events(aggregate_id, event_type);

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

CREATE OR REPLACE FUNCTION ensure_single_default()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.is_default = TRUE THEN
        IF TG_TABLE_NAME = 'addresses' THEN
            UPDATE addresses 
            SET is_default = FALSE 
            WHERE user_id = NEW.user_id 
            AND id != NEW.id 
            AND deleted_at IS NULL;
        ELSIF TG_TABLE_NAME = 'cards' THEN
            UPDATE cards 
            SET is_default = FALSE 
            WHERE user_id = NEW.user_id 
            AND id != NEW.id 
            AND deleted_at IS NULL;
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- FUNÇÃO CORRIGIDA: agora usa user_id em vez de id
CREATE OR REPLACE FUNCTION publish_user_event()
RETURNS TRIGGER AS $$
DECLARE
    event_type_name VARCHAR(100);
    event_payload JSONB;
    current_user_id VARCHAR(450);
BEGIN
    -- Define o user_id correto
    current_user_id := COALESCE(NEW.user_id, OLD.user_id);
    
    -- Ignora eventos de registros soft-deleted
    IF TG_OP = 'UPDATE' AND NEW.deleted_at IS NOT NULL THEN
        RETURN NEW;
    END IF;
    
    -- Determina o tipo de evento
    IF TG_OP = 'INSERT' THEN
        event_type_name := 'UserCreated';
        event_payload := to_jsonb(NEW);
    ELSIF TG_OP = 'UPDATE' THEN
        event_type_name := 'UserUpdated';
        event_payload := jsonb_build_object(
            'before', to_jsonb(OLD),
            'after', to_jsonb(NEW)
        );
    ELSIF TG_OP = 'DELETE' THEN
        event_type_name := 'UserDeleted';
        event_payload := to_jsonb(OLD);
    END IF;

    -- Insere no outbox
    INSERT INTO outbox_events (
        aggregate_id,
        aggregate_type,
        event_type,
        payload,
        metadata
    ) VALUES (
        current_user_id,
        'USER',
        event_type_name,
        event_payload,
        jsonb_build_object(
            'user_id', current_user_id,
            'correlation_id', uuid_generate_v4()
        )
    );

    RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- TRIGGERS
-- ========================================

CREATE TRIGGER update_users_updated_at 
    BEFORE UPDATE ON users 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_refresh_tokens_updated_at 
    BEFORE UPDATE ON refresh_tokens 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_addresses_updated_at 
    BEFORE UPDATE ON addresses 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_cards_updated_at 
    BEFORE UPDATE ON cards 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_users_version 
    BEFORE UPDATE ON users 
    FOR EACH ROW EXECUTE FUNCTION increment_version();

CREATE TRIGGER update_addresses_version 
    BEFORE UPDATE ON addresses 
    FOR EACH ROW EXECUTE FUNCTION increment_version();

CREATE TRIGGER update_cards_version 
    BEFORE UPDATE ON cards 
    FOR EACH ROW EXECUTE FUNCTION increment_version();

CREATE TRIGGER ensure_single_default_address 
    BEFORE INSERT OR UPDATE ON addresses 
    FOR EACH ROW EXECUTE FUNCTION ensure_single_default();

CREATE TRIGGER ensure_single_default_card 
    BEFORE INSERT OR UPDATE ON cards 
    FOR EACH ROW EXECUTE FUNCTION ensure_single_default();

CREATE TRIGGER publish_user_created 
    AFTER INSERT ON users 
    FOR EACH ROW EXECUTE FUNCTION publish_user_event();

CREATE TRIGGER publish_user_updated 
    AFTER UPDATE ON users 
    FOR EACH ROW EXECUTE FUNCTION publish_user_event();

-- ========================================
-- VIEWS (CORRIGIDA)
-- ========================================

CREATE OR REPLACE VIEW user_profiles AS
SELECT 
    u.user_id,  -- CORRIGIDO
    u.username,
    u.email,
    u.is_active,
    u.is_verified,
    u.last_login,
    u.created_at,
    
    jsonb_build_object(
        'id', a.id,
        'street', a.street,
        'number', a.number,
        'city', a.city,
        'state', a.state,
        'postal_code', a.postal_code,
        'country', a.country
    ) AS default_address,
    
    jsonb_build_object(
        'id', c.id,
        'last4', c.card_last4,
        'brand', c.card_brand,
        'cardholder', c.cardholder_name
    ) AS default_card
    
FROM users u
LEFT JOIN addresses a ON a.user_id = u.user_id AND a.is_default = TRUE AND a.deleted_at IS NULL
LEFT JOIN cards c ON c.user_id = u.user_id AND c.is_default = TRUE AND c.deleted_at IS NULL
WHERE u.deleted_at IS NULL;

-- ========================================
-- COMMENTS
-- ========================================

COMMENT ON TABLE users IS 'Tabela principal de usuários (ASP.NET Identity)';
COMMENT ON TABLE outbox_events IS 'Outbox pattern para garantir publicação de eventos';
COMMENT ON TABLE inbox_events IS 'Inbox pattern para deduplicação de eventos';
COMMENT ON COLUMN users.version IS 'Optimistic locking';
COMMENT ON COLUMN users.user_id IS 'PK compatível com ASP.NET Identity (string)';