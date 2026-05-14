START TRANSACTION;

DROP INDEX IF EXISTS public.uk_flag_nome_ambiente_filial;

ALTER TABLE IF EXISTS public.agendamentos DROP CONSTRAINT IF EXISTS ex_ag_veiculo_janela;

DROP TABLE public.agendamento_historico;

DROP TABLE public.agendamento_itens;

DROP TABLE public.audit_logs;

DROP TABLE public.feature_flags;

DROP TABLE public.notificacoes;

DROP TABLE public.outbox_eventos;

DROP TABLE public.usuario_preferencias;

DROP TABLE public.usuario_sessoes;

DROP TABLE public.servicos;

DROP TABLE public.agendamentos;

DROP TABLE public.usuarios;

DROP TABLE public.filiais;

DROP TABLE public.filiados;

DROP TABLE public.veiculos;

DROP TABLE public.clientes;

DELETE FROM public.__ef_migrations_history
WHERE migration_id = '20260513114525_InitialSchema';

COMMIT;

