-- Seed de demonstração — passo 2 (após scripts/seed-demo.mjs).
--
-- A API só aceita agendamento com início futuro, então o histórico é
-- fabricado aqui: ~70% dos agendados distantes (> 5 dias) são deslocados
-- 47 dias para trás e concluídos, com eventos INICIADO/FINALIZADO no
-- histórico (RN007). Metade dos cancelados também vai para o passado.
--
-- Uso:
--   docker exec -i carwash-postgres psql -U carwash_owner -d carwash < scripts/seed-demo-shift.sql

BEGIN;

-- 1) Conclui no passado uma fatia dos agendamentos futuros distantes.
WITH candidatos AS (
    SELECT id
    FROM public.agendamentos
    WHERE status = 'agendado'
      AND inicio > now() + interval '5 days'
),
alvo AS (
    SELECT id
    FROM candidatos
    ORDER BY md5(id::text)
    LIMIT (SELECT (count(*) * 0.70)::int FROM candidatos)
)
UPDATE public.agendamentos a
SET inicio        = a.inicio - interval '47 days',
    fim           = a.fim - interval '47 days',
    status        = 'finalizado',
    atualizado_em = now(),
    versao        = a.versao + 1
FROM alvo
WHERE a.id = alvo.id;

-- 2) Histórico do ciclo (RN007): INICIADO no começo e FINALIZADO no fim
--    da janela, para os concluídos que ainda não têm esses eventos.
INSERT INTO public.agendamento_historico (id, agendamento_id, evento, usuario_id, payload, ocorrido_em)
SELECT gen_random_uuid(),
       a.id,
       'INICIADO',
       '00000000-0000-0000-0000-000000000001',
       jsonb_build_object('statusAnterior', 'agendado', 'statusNovo', 'em_andamento', 'origem', 'seed-demo'),
       a.inicio
FROM public.agendamentos a
WHERE a.status = 'finalizado'
  AND a.fim < now()
  AND NOT EXISTS (
      SELECT 1 FROM public.agendamento_historico h
      WHERE h.agendamento_id = a.id AND h.evento = 'INICIADO');

INSERT INTO public.agendamento_historico (id, agendamento_id, evento, usuario_id, payload, ocorrido_em)
SELECT gen_random_uuid(),
       a.id,
       'FINALIZADO',
       '00000000-0000-0000-0000-000000000001',
       jsonb_build_object('statusAnterior', 'em_andamento', 'statusNovo', 'finalizado', 'origem', 'seed-demo'),
       a.fim
FROM public.agendamentos a
WHERE a.status = 'finalizado'
  AND a.fim < now()
  AND NOT EXISTS (
      SELECT 1 FROM public.agendamento_historico h
      WHERE h.agendamento_id = a.id AND h.evento = 'FINALIZADO');

-- 3) Metade dos cancelamentos também vira histórico (passado).
UPDATE public.agendamentos a
SET inicio        = a.inicio - interval '47 days',
    fim           = a.fim - interval '47 days',
    cancelado_em  = least(a.cancelado_em, a.inicio - interval '48 days'),
    atualizado_em = now(),
    versao        = a.versao + 1
WHERE a.status = 'cancelado'
  AND a.inicio > now()
  AND right(a.id::text, 1) IN ('0', '1', '2', '3', '4', '5', '6', '7');

COMMIT;

-- Resumo pós-seed
SELECT status, count(*) AS qtd,
       to_char(sum(valor_total), 'FM999G999D00') AS valor_total
FROM public.agendamentos
GROUP BY status
ORDER BY status;
