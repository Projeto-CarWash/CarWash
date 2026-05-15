START TRANSACTION;

DROP INDEX IF EXISTS public.uk_flag_nome_ambiente_filial;

ALTER TABLE IF EXISTS public.agendamentos DROP CONSTRAINT IF EXISTS ex_ag_veiculo_janela;

DROP TABLE IF EXISTS public.agendamento_historico;

DROP TABLE IF EXISTS public.agendamento_itens;

DROP TABLE IF EXISTS public.audit_logs;

DROP TABLE IF EXISTS public.feature_flags;

DROP TABLE IF EXISTS public.notificacoes;

DROP TABLE IF EXISTS public.outbox_eventos;

DROP TABLE IF EXISTS public.usuario_preferencias;

DROP TABLE IF EXISTS public.usuario_sessoes;

DROP TABLE IF EXISTS public.servicos;

DROP TABLE IF EXISTS public.agendamentos;

DROP TABLE IF EXISTS public.usuarios;

DROP TABLE IF EXISTS public.filiais;

DROP TABLE IF EXISTS public.filiados;

DROP TABLE IF EXISTS public.veiculos;

DROP TABLE IF EXISTS public.clientes;

DELETE FROM public.__ef_migrations_history
WHERE migration_id = '20260513114525_InitialSchema';

COMMIT;
