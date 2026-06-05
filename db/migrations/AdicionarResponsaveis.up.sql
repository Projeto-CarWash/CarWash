START TRANSACTION;

CREATE TABLE public.responsaveis (
    id uuid NOT NULL,
    cliente_titular_id uuid NOT NULL,
    nome character varying(100) NOT NULL,
    documento character varying(14) NOT NULL,
    telefone character varying(11),
    email character varying(150),
    grau_vinculo character varying(30) NOT NULL,
    ativo boolean NOT NULL DEFAULT TRUE,
    criado_em timestamptz NOT NULL DEFAULT (now()),
    atualizado_em timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_responsaveis PRIMARY KEY (id),
    CONSTRAINT ck_responsaveis_grau_vinculo CHECK (grau_vinculo IN ('RESPONSAVEL_FINANCEIRO','RESPONSAVEL_LEGAL','PROCURADOR','CONJUGE','PAI_MAE','OUTRO')),
    CONSTRAINT fk_responsaveis_cliente_titular FOREIGN KEY (cliente_titular_id) REFERENCES public.clientes (id) ON DELETE RESTRICT
);

CREATE UNIQUE INDEX uk_responsaveis_documento ON public.responsaveis (documento);

CREATE INDEX idx_responsaveis_cliente_titular_id ON public.responsaveis (cliente_titular_id);

INSERT INTO public.__ef_migrations_history (migration_id, product_version) VALUES ('20260601120000_AdicionarResponsaveis', '8.0.10');

COMMIT;
