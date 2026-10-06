-- ============================================================
-- Crivo MVP — Script de criação do banco de dados
-- PostgreSQL 16
-- ============================================================

-- Extensão para geração de UUIDs
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================================
-- Tabela: tenants (Empresas)
-- ============================================================
CREATE TABLE IF NOT EXISTS tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    slug VARCHAR(100) NOT NULL UNIQUE,
    uninstall_password_hash VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- Tabela: sectors (Setores / Departamentos)
-- ============================================================
CREATE TABLE IF NOT EXISTS sectors (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name VARCHAR(150) NOT NULL,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (tenant_id, name)
);

CREATE INDEX IF NOT EXISTS idx_sectors_tenant ON sectors(tenant_id);

-- ============================================================
-- Tabela: users (Usuários do Painel Web)
-- ============================================================
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    email VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    name VARCHAR(200) NOT NULL,
    role VARCHAR(20) NOT NULL DEFAULT 'Viewer',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (tenant_id, email)
);

CREATE INDEX IF NOT EXISTS idx_users_tenant ON users(tenant_id);

-- ============================================================
-- Tabela: machines (Máquinas Registradas)
-- ============================================================
CREATE TABLE IF NOT EXISTS machines (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    sector_id UUID NOT NULL REFERENCES sectors(id),
    asset_number VARCHAR(50) NOT NULL,
    employee_name VARCHAR(200) NOT NULL,
    hostname VARCHAR(255),
    hardware_fingerprint VARCHAR(512),
    agent_version VARCHAR(50),
    connection_status VARCHAR(20) NOT NULL DEFAULT 'Offline',
    active_rule_hash VARCHAR(128),
    last_heartbeat TIMESTAMPTZ,
    registered_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (tenant_id, asset_number)
);

CREATE INDEX IF NOT EXISTS idx_machines_tenant_sector ON machines(tenant_id, sector_id);
CREATE INDEX IF NOT EXISTS idx_machines_status ON machines(connection_status);

-- ============================================================
-- Tabela: rules (Regras de Bloqueio/Liberação)
-- ============================================================
CREATE TABLE IF NOT EXISTS rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name VARCHAR(200) NOT NULL,
    scope_type VARCHAR(20) NOT NULL CHECK (scope_type IN ('Global', 'Sector', 'Machine')),
    scope_id UUID,
    action VARCHAR(10) NOT NULL CHECK (action IN ('Block', 'Allow')),
    priority INT NOT NULL DEFAULT 100,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_temporary BOOLEAN NOT NULL DEFAULT FALSE,
    expires_at TIMESTAMPTZ,
    domains JSONB NOT NULL DEFAULT '[]',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_rules_lookup ON rules(tenant_id, scope_type, scope_id) WHERE is_active = TRUE;
CREATE INDEX IF NOT EXISTS idx_rules_domains ON rules USING gin(domains);

-- ============================================================
-- Tabela: block_events (Log de Bloqueios)
-- Particionada por mês para performance
-- ============================================================
CREATE TABLE IF NOT EXISTS block_events (
    id BIGSERIAL,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    machine_id UUID NOT NULL REFERENCES machines(id),
    rule_id UUID REFERENCES rules(id),
    blocked_domain VARCHAR(500) NOT NULL,
    blocked_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    process_name VARCHAR(255),
    PRIMARY KEY (id, blocked_at)
) PARTITION BY RANGE (blocked_at);

-- Partições para os próximos meses
CREATE TABLE IF NOT EXISTS block_events_2026_08 PARTITION OF block_events
    FOR VALUES FROM ('2026-08-01') TO ('2026-09-01');
CREATE TABLE IF NOT EXISTS block_events_2026_09 PARTITION OF block_events
    FOR VALUES FROM ('2026-09-01') TO ('2026-10-01');
CREATE TABLE IF NOT EXISTS block_events_2026_10 PARTITION OF block_events
    FOR VALUES FROM ('2026-10-01') TO ('2026-11-01');
CREATE TABLE IF NOT EXISTS block_events_2026_11 PARTITION OF block_events
    FOR VALUES FROM ('2026-11-01') TO ('2026-12-01');
CREATE TABLE IF NOT EXISTS block_events_2026_12 PARTITION OF block_events
    FOR VALUES FROM ('2026-12-01') TO ('2027-01-01');

CREATE INDEX IF NOT EXISTS idx_block_events_tenant ON block_events(tenant_id, blocked_at DESC);
CREATE INDEX IF NOT EXISTS idx_block_events_machine ON block_events(machine_id, blocked_at DESC);

-- ============================================================
-- Dados de exemplo para desenvolvimento
-- ============================================================

-- Empresa de exemplo
-- Senha de desinstalação: "CriveAdmin2024!" (hash BCrypt)
INSERT INTO tenants (id, name, slug, uninstall_password_hash) VALUES
    ('a0000000-0000-0000-0000-000000000001', 'Empresa Exemplo LTDA', 'exemplo',
     '$2a$11$K8rGjY0kGxHWqKBGhLmPxeVCHJZpLBkXKJGQ5YB1xL9Qz7CvGhT8e')
ON CONFLICT (slug) DO NOTHING;

-- Setores de exemplo
INSERT INTO sectors (id, tenant_id, name, description) VALUES
    ('b0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001', 'RH', 'Recursos Humanos'),
    ('b0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', 'Financeiro', 'Departamento Financeiro'),
    ('b0000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000001', 'Vendas', 'Equipe de Vendas'),
    ('b0000000-0000-0000-0000-000000000004', 'a0000000-0000-0000-0000-000000000001', 'TI', 'Tecnologia da Informação'),
    ('b0000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000001', 'Marketing', 'Departamento de Marketing')
ON CONFLICT (tenant_id, name) DO NOTHING;

-- Usuário admin de exemplo
-- Senha: "crivo6968" (hash BCrypt)
INSERT INTO users (id, tenant_id, email, password_hash, name, role) VALUES
    ('c0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001',
     'adminbry@crivo.com',
     '$2a$11$mhBw4512hoRxIFXyLsmn9OtNev.FcI6A8TnxEdv5KZ54gvt3qDhSu',
     'Bryan Admin', 'Admin')
ON CONFLICT (tenant_id, email) DO NOTHING;

-- Regras globais de exemplo
INSERT INTO rules (id, tenant_id, name, scope_type, action, priority, domains) VALUES
    ('d0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001',
     'Bloquear Sites de Apostas', 'Global', 'Block', 1000,
     '["bet365.com", "betano.com", "sportingbet.com", "pixbet.com", "betfair.com", "stake.com", "blaze.com"]'),
    ('d0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001',
     'Bloquear Redes Sociais', 'Global', 'Block', 1000,
     '["youtube.com", "tiktok.com", "instagram.com", "facebook.com", "twitter.com", "x.com", "reddit.com"]'),
    ('d0000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000001',
     'Bloquear Streaming', 'Global', 'Block', 1000,
     '["netflix.com", "twitch.tv", "primevideo.com", "hbomax.com", "globoplay.globo.com"]')
ON CONFLICT DO NOTHING;

-- Exceção: Marketing pode usar YouTube e Instagram
INSERT INTO rules (id, tenant_id, name, scope_type, scope_id, action, priority, domains) VALUES
    ('d0000000-0000-0000-0000-000000000004', 'a0000000-0000-0000-0000-000000000001',
     'Marketing - Liberar Redes Sociais', 'Sector', 'b0000000-0000-0000-0000-000000000005',
     'Allow', 500,
     '["youtube.com", "instagram.com"]')
ON CONFLICT DO NOTHING;

-- ============================================================
-- Comentário sobre partições futuras:
-- Criar uma função/cron job para gerar partições mensais automaticamente:
-- CREATE TABLE block_events_YYYY_MM PARTITION OF block_events
--     FOR VALUES FROM ('YYYY-MM-01') TO ('YYYY-MM+1-01');
-- ============================================================
