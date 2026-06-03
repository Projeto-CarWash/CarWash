START TRANSACTION;

DROP TABLE IF EXISTS public.responsaveis;

DELETE FROM public.__ef_migrations_history WHERE migration_id = '20260601120000_AdicionarResponsaveis';

COMMIT;
