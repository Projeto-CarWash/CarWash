CREATE TABLE IF NOT EXISTS public.__ef_migrations_history (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

CREATE EXTENSION IF NOT EXISTS btree_gist;

CREATE TABLE public.clientes (
    id uuid NOT NULL,
    nome character varying(100) NOT NULL,
    cpf character varying(11),
    cnpj character varying(14),
    telefone character varying(11),
    celular character varying(11),
    email character varying(150),
    endereco character varying(255),
    observacoes text,
    ativo boolean NOT NULL DEFAULT TRUE,
    criado_em timestamptz NOT NULL DEFAULT (now()),
    atualizado_em timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_clientes PRIMARY KEY (id),
    CONSTRAINT ck_clientes_cpf_ou_cnpj CHECK (cpf IS NOT NULL OR cnpj IS NOT NULL)
);

CREATE TABLE public.filiais (
    id uuid NOT NULL,
    nome character varying(120) NOT NULL,
    ativa boolean NOT NULL DEFAULT TRUE,
    celulas_ativas integer NOT NULL,
    timezone character varying(64) NOT NULL DEFAULT 'America/Sao_Paulo',
    criado_em timestamptz NOT NULL DEFAULT (now()),
    atualizado_em timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_filiais PRIMARY KEY (id),
    CONSTRAINT ck_filiais_celulas_faixa CHECK (celulas_ativas BETWEEN 1 AND 100)
);

CREATE TABLE public.outbox_eventos (
    id uuid NOT NULL,
    evento character varying(80) NOT NULL,
    agregado character varying(80) NOT NULL,
    agregado_id uuid NOT NULL,
    payload jsonb NOT NULL,
    idempotency_key character varying(120) NOT NULL,
    status character varying(20) NOT NULL DEFAULT 'pendente',
    tentativas integer NOT NULL DEFAULT 0,
    disponivel_em timestamptz NOT NULL DEFAULT (now()),
    criado_em timestamptz NOT NULL DEFAULT (now()),
    processado_em timestamptz,
    CONSTRAINT pk_outbox_eventos PRIMARY KEY (id),
    CONSTRAINT ck_outbox_status CHECK (status IN ('pendente','processando','processado','falha')),
    CONSTRAINT ck_outbox_tentativas CHECK (tentativas >= 0)
);

CREATE TABLE public.servicos (
    id uuid NOT NULL,
    nome character varying(120) NOT NULL,
    preco numeric(10,2) NOT NULL,
    duracao_min integer NOT NULL,
    ativo boolean NOT NULL DEFAULT TRUE,
    criado_em timestamptz NOT NULL DEFAULT (now()),
    atualizado_em timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_servicos PRIMARY KEY (id),
    CONSTRAINT ck_servicos_duracao CHECK (duracao_min > 0),
    CONSTRAINT ck_servicos_preco CHECK (preco > 0)
);

CREATE TABLE public.usuarios (
    id uuid NOT NULL,
    nome character varying(120) NOT NULL,
    email character varying(150) NOT NULL,
    senha_hash text NOT NULL,
    perfil character varying(20) NOT NULL,
    ativo boolean NOT NULL DEFAULT TRUE,
    criado_em timestamptz NOT NULL DEFAULT (now()),
    atualizado_em timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_usuarios PRIMARY KEY (id),
    CONSTRAINT ck_usuarios_perfil CHECK (perfil IN ('ADMIN','FUNCIONARIO'))
);

CREATE TABLE public.filiados (
    id uuid NOT NULL,
    cliente_id uuid NOT NULL,
    nome character varying(120) NOT NULL,
    telefone character varying(11) NOT NULL,
    cpf character varying(11),
    rg character varying(20),
    ativo boolean NOT NULL DEFAULT TRUE,
    criado_em timestamptz NOT NULL DEFAULT (now()),
    atualizado_em timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_filiados PRIMARY KEY (id),
    CONSTRAINT ck_filiados_cpf_ou_rg CHECK (cpf IS NOT NULL OR rg IS NOT NULL),
    CONSTRAINT fk_filiados_cliente FOREIGN KEY (cliente_id) REFERENCES public.clientes (id) ON DELETE RESTRICT
);

CREATE TABLE public.veiculos (
    id uuid NOT NULL,
    cliente_id uuid NOT NULL,
    placa character varying(10) NOT NULL,
    modelo character varying(80) NOT NULL,
    fabricante character varying(80) NOT NULL,
    cor character varying(40) NOT NULL,
    ano integer,
    ativo boolean NOT NULL DEFAULT TRUE,
    criado_em timestamptz NOT NULL DEFAULT (now()),
    atualizado_em timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_veiculos PRIMARY KEY (id),
    CONSTRAINT ck_veiculos_ano CHECK (ano IS NULL OR (ano BETWEEN 1900 AND 2100)),
    CONSTRAINT fk_veiculos_cliente FOREIGN KEY (cliente_id) REFERENCES public.clientes (id) ON DELETE RESTRICT
);

CREATE TABLE public.audit_logs (
    id uuid NOT NULL,
    evento character varying(80) NOT NULL,
    entidade character varying(80) NOT NULL,
    entidade_id uuid,
    usuario_id uuid,
    correlation_id character varying(64) NOT NULL,
    dados jsonb,
    criado_em timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_audit_logs PRIMARY KEY (id),
    CONSTRAINT fk_audit_usuario FOREIGN KEY (usuario_id) REFERENCES public.usuarios (id) ON DELETE SET NULL
);

CREATE TABLE public.feature_flags (
    id uuid NOT NULL,
    nome character varying(100) NOT NULL,
    ambiente character varying(30) NOT NULL,
    filial_id uuid,
    habilitada boolean NOT NULL DEFAULT FALSE,
    valor_json jsonb,
    atualizado_por uuid NOT NULL,
    atualizado_em timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_feature_flags PRIMARY KEY (id),
    CONSTRAINT fk_flag_atualizado_por FOREIGN KEY (atualizado_por) REFERENCES public.usuarios (id) ON DELETE RESTRICT,
    CONSTRAINT fk_flag_filial FOREIGN KEY (filial_id) REFERENCES public.filiais (id) ON DELETE CASCADE
);

CREATE TABLE public.usuario_preferencias (
    id uuid NOT NULL,
    usuario_id uuid NOT NULL,
    tema character varying(10) NOT NULL DEFAULT 'claro',
    atualizado_em timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_usuario_preferencias PRIMARY KEY (id),
    CONSTRAINT ck_pref_tema CHECK (tema IN ('claro','escuro')),
    CONSTRAINT fk_pref_usuario FOREIGN KEY (usuario_id) REFERENCES public.usuarios (id) ON DELETE CASCADE
);

CREATE TABLE public.usuario_sessoes (
    id uuid NOT NULL,
    usuario_id uuid NOT NULL,
    refresh_token_hash text NOT NULL,
    expira_em timestamptz NOT NULL,
    revogado_em timestamptz,
    ip_origem character varying(45),
    user_agent character varying(500),
    criado_em timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_usuario_sessoes PRIMARY KEY (id),
    CONSTRAINT fk_sessoes_usuario FOREIGN KEY (usuario_id) REFERENCES public.usuarios (id) ON DELETE CASCADE
);

CREATE TABLE public.agendamentos (
    id uuid NOT NULL,
    filial_id uuid NOT NULL,
    cliente_id uuid NOT NULL,
    veiculo_id uuid NOT NULL,
    responsavel_id uuid,
    criado_por uuid NOT NULL,
    status character varying(20) NOT NULL DEFAULT 'agendado',
    inicio timestamptz NOT NULL,
    fim timestamptz NOT NULL,
    observacoes text,
    versao integer NOT NULL DEFAULT 1,
    criado_em timestamptz NOT NULL DEFAULT (now()),
    atualizado_em timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_agendamentos PRIMARY KEY (id),
    CONSTRAINT ck_ag_inicio_menor_fim CHECK (inicio < fim),
    CONSTRAINT ck_ag_status CHECK (status IN ('agendado','cancelado','finalizado')),
    CONSTRAINT fk_ag_cliente FOREIGN KEY (cliente_id) REFERENCES public.clientes (id) ON DELETE RESTRICT,
    CONSTRAINT fk_ag_criado_por FOREIGN KEY (criado_por) REFERENCES public.usuarios (id) ON DELETE RESTRICT,
    CONSTRAINT fk_ag_filial FOREIGN KEY (filial_id) REFERENCES public.filiais (id) ON DELETE RESTRICT,
    CONSTRAINT fk_ag_responsavel FOREIGN KEY (responsavel_id) REFERENCES public.filiados (id) ON DELETE RESTRICT,
    CONSTRAINT fk_ag_veiculo FOREIGN KEY (veiculo_id) REFERENCES public.veiculos (id) ON DELETE RESTRICT
);

CREATE TABLE public.agendamento_historico (
    id uuid NOT NULL,
    agendamento_id uuid NOT NULL,
    evento character varying(50) NOT NULL,
    payload jsonb,
    usuario_id uuid NOT NULL,
    ocorrido_em timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_agendamento_historico PRIMARY KEY (id),
    CONSTRAINT ck_hist_evento CHECK (evento IN ('CRIADO','EDITADO','CANCELADO','FINALIZADO')),
    CONSTRAINT fk_hist_agendamento FOREIGN KEY (agendamento_id) REFERENCES public.agendamentos (id) ON DELETE CASCADE,
    CONSTRAINT fk_hist_usuario FOREIGN KEY (usuario_id) REFERENCES public.usuarios (id) ON DELETE RESTRICT
);

CREATE TABLE public.agendamento_itens (
    id uuid NOT NULL,
    agendamento_id uuid NOT NULL,
    servico_id uuid NOT NULL,
    preco_aplicado numeric(10,2) NOT NULL,
    duracao_aplicada integer NOT NULL,
    criado_em timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_agendamento_itens PRIMARY KEY (id),
    CONSTRAINT ck_item_duracao CHECK (duracao_aplicada > 0),
    CONSTRAINT ck_item_preco CHECK (preco_aplicado >= 0),
    CONSTRAINT fk_item_agendamento FOREIGN KEY (agendamento_id) REFERENCES public.agendamentos (id) ON DELETE CASCADE,
    CONSTRAINT fk_item_servico FOREIGN KEY (servico_id) REFERENCES public.servicos (id) ON DELETE RESTRICT
);

CREATE TABLE public.notificacoes (
    id uuid NOT NULL,
    agendamento_id uuid NOT NULL,
    tipo character varying(30) NOT NULL,
    canal character varying(20) NOT NULL,
    destino character varying(120) NOT NULL,
    idempotency_key character varying(120) NOT NULL,
    status character varying(20) NOT NULL DEFAULT 'pendente',
    tentativas integer NOT NULL DEFAULT 0,
    ultima_tentativa timestamptz,
    criado_em timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_notificacoes PRIMARY KEY (id),
    CONSTRAINT ck_notif_canal CHECK (canal IN ('email','whatsapp','sms')),
    CONSTRAINT ck_notif_tentativas CHECK (tentativas >= 0),
    CONSTRAINT fk_notif_agendamento FOREIGN KEY (agendamento_id) REFERENCES public.agendamentos (id) ON DELETE CASCADE
);

CREATE INDEX idx_hist_agendamento ON public.agendamento_historico (agendamento_id, ocorrido_em DESC);

CREATE INDEX idx_hist_evento ON public.agendamento_historico (evento);

CREATE INDEX idx_hist_ocorrido_em ON public.agendamento_historico (ocorrido_em);

CREATE INDEX ix_agendamento_historico_usuario_id ON public.agendamento_historico (usuario_id);

CREATE INDEX idx_item_agendamento ON public.agendamento_itens (agendamento_id);

CREATE INDEX idx_item_servico ON public.agendamento_itens (servico_id);

CREATE UNIQUE INDEX uk_item_agendamento_servico ON public.agendamento_itens (agendamento_id, servico_id);

CREATE INDEX idx_ag_cliente ON public.agendamentos (cliente_id, inicio DESC);

CREATE INDEX idx_ag_filial_inicio ON public.agendamentos (filial_id, inicio);

CREATE INDEX idx_ag_status ON public.agendamentos (status);

CREATE INDEX idx_ag_veiculo ON public.agendamentos (veiculo_id, inicio);

CREATE INDEX ix_agendamentos_criado_por ON public.agendamentos (criado_por);

CREATE INDEX ix_agendamentos_responsavel_id ON public.agendamentos (responsavel_id);

CREATE INDEX idx_audit_correlation ON public.audit_logs (correlation_id);

CREATE INDEX idx_audit_criado_em ON public.audit_logs (criado_em DESC);

CREATE INDEX idx_audit_entidade ON public.audit_logs (entidade, entidade_id);

CREATE INDEX idx_audit_evento ON public.audit_logs (evento, criado_em DESC);

CREATE INDEX ix_audit_logs_usuario_id ON public.audit_logs (usuario_id);

CREATE INDEX idx_clientes_email ON public.clientes (email) WHERE email IS NOT NULL;

CREATE INDEX idx_clientes_nome ON public.clientes (nome);

CREATE UNIQUE INDEX uk_clientes_cnpj ON public.clientes (cnpj) WHERE cnpj IS NOT NULL;

CREATE UNIQUE INDEX uk_clientes_cpf ON public.clientes (cpf) WHERE cpf IS NOT NULL;

CREATE INDEX ix_feature_flags_atualizado_por ON public.feature_flags (atualizado_por);

CREATE INDEX ix_feature_flags_filial_id ON public.feature_flags (filial_id);

CREATE INDEX idx_filiados_cliente_id ON public.filiados (cliente_id);

CREATE INDEX idx_filiados_cpf ON public.filiados (cpf) WHERE cpf IS NOT NULL;

CREATE UNIQUE INDEX uk_filiados_cliente_cpf ON public.filiados (cliente_id, cpf) WHERE cpf IS NOT NULL;

CREATE INDEX idx_filiais_ativa ON public.filiais (ativa) WHERE ativa = true;

CREATE UNIQUE INDEX uk_filiais_nome ON public.filiais (nome);

CREATE INDEX idx_notif_status ON public.notificacoes (status, criado_em);

CREATE UNIQUE INDEX uk_notif_dedupe ON public.notificacoes (agendamento_id, tipo, canal, idempotency_key);

CREATE INDEX idx_outbox_status_disponivel ON public.outbox_eventos (status, disponivel_em) WHERE status IN ('pendente','falha');

CREATE UNIQUE INDEX uk_outbox_idempotency ON public.outbox_eventos (idempotency_key);

CREATE INDEX idx_servicos_ativo ON public.servicos (ativo) WHERE ativo = true;

CREATE UNIQUE INDEX uk_servicos_nome ON public.servicos (nome);

CREATE UNIQUE INDEX uk_pref_usuario_id ON public.usuario_preferencias (usuario_id);

CREATE INDEX idx_sessoes_expira_em ON public.usuario_sessoes (expira_em);

CREATE INDEX idx_sessoes_revogado_em ON public.usuario_sessoes (revogado_em) WHERE revogado_em IS NOT NULL;

CREATE INDEX idx_sessoes_usuario_id ON public.usuario_sessoes (usuario_id);

CREATE INDEX idx_usuarios_ativo ON public.usuarios (ativo) WHERE ativo = false;

CREATE UNIQUE INDEX uk_usuarios_email ON public.usuarios (email);

CREATE INDEX idx_veiculos_cliente_id ON public.veiculos (cliente_id);

CREATE UNIQUE INDEX uk_veiculos_placa ON public.veiculos (placa);


ALTER TABLE public.agendamentos
ADD CONSTRAINT ex_ag_veiculo_janela
EXCLUDE USING gist (
    veiculo_id WITH =,
    tstzrange(inicio, fim, '[)') WITH &&
)
WHERE (status = 'agendado');


CREATE UNIQUE INDEX uk_flag_nome_ambiente_filial
ON public.feature_flags (
    nome,
    ambiente,
    COALESCE(filial_id, '00000000-0000-0000-0000-000000000000'::uuid)
);

INSERT INTO public.usuarios (id, nome, email, senha_hash, perfil, ativo, criado_em, atualizado_em)
VALUES ('00000000-0000-0000-0000-000000000001', 'Administrador', 'admin@carwash.local', '$argon2id$v=19$m=65536,t=3,p=1$8kWRCni3FmpP5fi7TAPCeQ$BiEQqNMXyHIgSXja50Q63ALURbXKKB98kN6yNlyJguE', 'ADMIN', TRUE, TIMESTAMPTZ '2026-05-13T11:52:00.230971Z', TIMESTAMPTZ '2026-05-13T11:52:00.230971Z');

INSERT INTO public.filiais (id, nome, ativa, celulas_ativas, timezone, criado_em, atualizado_em)
VALUES ('00000000-0000-0000-0000-000000000010', 'Matriz', TRUE, 4, 'America/Sao_Paulo', TIMESTAMPTZ '2026-05-13T11:52:00.230971Z', TIMESTAMPTZ '2026-05-13T11:52:00.230971Z');

INSERT INTO public.servicos (id, nome, preco, duracao_min, ativo, criado_em, atualizado_em)
VALUES ('00000000-0000-0000-0000-000000000100', 'Lavagem Simples', 30.0, 30, TRUE, TIMESTAMPTZ '2026-05-13T11:52:00.230971Z', TIMESTAMPTZ '2026-05-13T11:52:00.230971Z');
INSERT INTO public.servicos (id, nome, preco, duracao_min, ativo, criado_em, atualizado_em)
VALUES ('00000000-0000-0000-0000-000000000101', 'Lavagem Completa', 60.0, 60, TRUE, TIMESTAMPTZ '2026-05-13T11:52:00.230971Z', TIMESTAMPTZ '2026-05-13T11:52:00.230971Z');
INSERT INTO public.servicos (id, nome, preco, duracao_min, ativo, criado_em, atualizado_em)
VALUES ('00000000-0000-0000-0000-000000000102', 'Enceramento', 45.0, 45, TRUE, TIMESTAMPTZ '2026-05-13T11:52:00.230971Z', TIMESTAMPTZ '2026-05-13T11:52:00.230971Z');

INSERT INTO public.usuario_preferencias (id, usuario_id, tema, atualizado_em)
VALUES ('00000000-0000-0000-0000-000000000200', '00000000-0000-0000-0000-000000000001', 'claro', TIMESTAMPTZ '2026-05-13T11:52:00.230971Z');

INSERT INTO public.__ef_migrations_history (migration_id, product_version)
VALUES ('20260513114525_InitialSchema', '8.0.10');

COMMIT;

