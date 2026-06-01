-- RF008: Permitir agendamentos simultâneos no mesmo horário com controle de capacidade.
-- 1. Adicionar colunas duracao_total_min e valor_total à tabela agendamentos.
-- 2. Atualizar CHECK de status para incluir 'em_andamento'.
-- 3. Atualizar EXCLUDE constraint para cobrir ambos os status que consomem capacidade.

BEGIN;

-- 1. Novas colunas
ALTER TABLE public.agendamentos
    ADD COLUMN duracao_total_min integer NOT NULL DEFAULT 0,
    ADD COLUMN valor_total numeric(10,2) NOT NULL DEFAULT 0.00;

-- Popula colunas novas com dados derivados dos itens existentes (retrocompatibilidade).
UPDATE public.agendamentos a
SET duracao_total_min = sub.dur,
    valor_total = sub.val
FROM (
    SELECT ai.agendamento_id,
           SUM(ai.duracao_aplicada) AS dur,
           SUM(ai.preco_aplicado) AS val
    FROM public.agendamento_itens ai
    GROUP BY ai.agendamento_id
) sub
WHERE a.id = sub.agendamento_id;

-- Remove o DEFAULT após o backfill para que novos registros sempre informem o valor.
ALTER TABLE public.agendamentos
    ALTER COLUMN duracao_total_min DROP DEFAULT,
    ALTER COLUMN valor_total DROP DEFAULT;

-- CHECK de duração e valor
ALTER TABLE public.agendamentos
    ADD CONSTRAINT ck_ag_duracao_positiva CHECK (duracao_total_min > 0),
    ADD CONSTRAINT ck_ag_valor_nao_negativo CHECK (valor_total >= 0);

-- 2. Atualizar CHECK de status para incluir 'em_andamento'
DROP CONSTRAINT IF EXISTS ck_ag_status ON public.agendamentos;
ALTER TABLE public.agendamentos
    ADD CONSTRAINT ck_ag_status CHECK (status IN ('agendado','em_andamento','cancelado','finalizado'));

-- 3. Atualizar EXCLUDE constraint para cobrir status que consomem capacidade
DROP CONSTRAINT IF EXISTS ex_ag_veiculo_janela ON public.agendamentos;
ALTER TABLE public.agendamentos
    ADD CONSTRAINT ex_ag_veiculo_janela
    EXCLUDE USING gist (
        veiculo_id WITH =,
        tstzrange(inicio, fim, '[)') WITH &&
    ) WHERE (status IN ('agendado', 'em_andamento'));

-- Registrar migration no EF history
INSERT INTO public.__ef_migrations_history (migration_id, product_version)
VALUES ('20260530000000_Rf008AgendamentoSimultaneo', '8.0.10');

COMMIT;
