-- RF008: Rollback — reverter agendamentos simultâneos.
BEGIN;

-- 1. Reverter EXCLUDE constraint para apenas 'agendado'
DROP CONSTRAINT IF EXISTS ex_ag_veiculo_janela ON public.agendamentos;
ALTER TABLE public.agendamentos
    ADD CONSTRAINT ex_ag_veiculo_janela
    EXCLUDE USING gist (
        veiculo_id WITH =,
        tstzrange(inicio, fim, '[)') WITH &&
    ) WHERE (status = 'agendado');

-- 2. Reverter CHECK de status para excluir 'em_andamento'
DROP CONSTRAINT IF EXISTS ck_ag_status ON public.agendamentos;
ALTER TABLE public.agendamentos
    ADD CONSTRAINT ck_ag_status CHECK (status IN ('agendado','cancelado','finalizado'));

-- 3. Remover colunas e CHECKs adicionados
ALTER TABLE public.agendamentos
    DROP CONSTRAINT IF EXISTS ck_ag_duracao_positiva,
    DROP CONSTRAINT IF EXISTS ck_ag_valor_nao_negativo;

ALTER TABLE public.agendamentos
    DROP COLUMN IF EXISTS duracao_total_min,
    DROP COLUMN IF EXISTS valor_total;

-- Remover migration do EF history
DELETE FROM public.__ef_migrations_history
WHERE migration_id = '20260530000000_Rf008AgendamentoSimultaneo';

COMMIT;
