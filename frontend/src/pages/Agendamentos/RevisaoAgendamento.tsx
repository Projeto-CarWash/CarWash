import {
  AlertCircle,
  Building2,
  CalendarClock,
  Car,
  Check,
  Clock,
  Loader2,
  Pencil,
  Receipt,
  User,
  X,
} from 'lucide-react';
import { useEffect, useRef } from 'react';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { formatarDataHora, formatarDuracao, formatarReais } from '@/lib/format';

import type { ResumoConfirmacao } from '@/types/agendamento';
import type { ReactNode } from 'react';

interface RevisaoAgendamentoProps {
  /** Resumo derivado pelo backend na etapa de pré-confirmação. */
  resumo: ResumoConfirmacao;
  /** Confirma e persiste o agendamento (`POST /confirmar`). */
  onConfirmar: () => void;
  /** Volta para a etapa de edição preservando o formulário. */
  onEditar: () => void;
  /** Indica que a confirmação está em andamento (desabilita os botões). */
  confirmando: boolean;
  /** Erro a ser exibido no banner da revisão (409 de conflito RN011, etc.). */
  erro?: string | null;
  /** Limpa o banner de erro. */
  onLimparErro?: () => void;
}

/**
 * Etapa de revisão do RF015 (card 133).
 *
 * <p>Renderiza o resumo devolvido pela pré-confirmação — filial, cliente,
 * veículo, serviços com duração e preço, horários e totais — e exige uma
 * confirmação explícita antes de persistir o agendamento. É DISTINTA do
 * `ResumoAgendamento`, que é apenas uma estimativa inline do formulário.</p>
 *
 * <p>O backend continua sendo a fonte de verdade: os totais aqui são os
 * derivados server-side e o `hashResumo` permite detectar divergência na
 * confirmação.</p>
 */
export function RevisaoAgendamento({
  resumo,
  onConfirmar,
  onEditar,
  confirmando,
  erro,
  onLimparErro,
}: RevisaoAgendamentoProps) {
  const tituloRef = useRef<HTMLHeadingElement>(null);

  // Foca o título ao entrar na etapa — leitores de tela anunciam a troca.
  useEffect(() => {
    tituloRef.current?.focus();
  }, []);

  return (
    <Card className="border border-zinc-800/60 bg-zinc-900/30">
      <CardHeader>
        <CardTitle ref={tituloRef} tabIndex={-1} className="text-lg text-zinc-100 outline-none">
          Revise antes de confirmar
        </CardTitle>
        <p className="text-sm text-zinc-400">
          Confira os dados abaixo. O agendamento só é registrado após a sua confirmação (RF015).
        </p>
      </CardHeader>
      <CardContent>
        {erro && (
          <div
            role="alert"
            aria-live="assertive"
            className="mb-6 flex items-start gap-3 rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3"
          >
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-red-500" aria-hidden="true" />
            <p className="flex-1 text-sm font-medium text-red-400">{erro}</p>
            {onLimparErro && (
              <button
                type="button"
                onClick={onLimparErro}
                aria-label="Fechar mensagem de erro"
                className="shrink-0 text-red-500/60 transition-colors hover:text-red-400"
              >
                <X className="h-4 w-4" aria-hidden="true" />
              </button>
            )}
          </div>
        )}

        <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <CampoRevisao
            icone={<Building2 className="h-4 w-4" aria-hidden="true" />}
            rotulo="Filial"
          >
            {resumo.filial.nome}
          </CampoRevisao>

          <CampoRevisao icone={<User className="h-4 w-4" aria-hidden="true" />} rotulo="Cliente">
            <span className="block">{resumo.cliente.nome}</span>
            <span className="block text-xs text-zinc-500">{resumo.cliente.documento}</span>
          </CampoRevisao>

          <CampoRevisao icone={<Car className="h-4 w-4" aria-hidden="true" />} rotulo="Veículo">
            <span className="block font-mono tracking-wide">{resumo.veiculo.placa}</span>
            <span className="block text-xs text-zinc-500">
              {resumo.veiculo.modelo} · {resumo.veiculo.cor}
            </span>
          </CampoRevisao>

          <CampoRevisao
            icone={<CalendarClock className="h-4 w-4" aria-hidden="true" />}
            rotulo="Início e fim"
          >
            <span className="block">{formatarDataHora(resumo.inicio)}</span>
            <span className="block text-xs text-zinc-500">até {formatarDataHora(resumo.fim)}</span>
          </CampoRevisao>
        </dl>

        {/* Serviços */}
        <div className="mt-6">
          <h3 className="text-sm font-semibold tracking-wide text-zinc-200">Serviços</h3>
          <ul className="mt-2 divide-y divide-zinc-800/60 rounded-xl border border-zinc-800/60">
            {resumo.servicos.map((servico) => (
              <li
                key={servico.id}
                className="flex items-center justify-between gap-3 px-4 py-3 text-sm"
              >
                <span className="min-w-0">
                  <span className="block truncate font-medium text-zinc-200">{servico.nome}</span>
                  <span className="mt-0.5 block text-xs text-zinc-500">
                    {formatarDuracao(servico.duracaoMin)}
                  </span>
                </span>
                <span className="shrink-0 tabular-nums text-zinc-300">
                  {formatarReais(servico.preco)}
                </span>
              </li>
            ))}
          </ul>
        </div>

        {/* Observações */}
        {resumo.observacoes && (
          <div className="mt-6">
            <h3 className="text-sm font-semibold tracking-wide text-zinc-200">Observações</h3>
            <p className="mt-2 rounded-xl border border-zinc-800/60 bg-zinc-950/40 px-4 py-3 text-sm whitespace-pre-line text-zinc-300">
              {resumo.observacoes}
            </p>
          </div>
        )}

        {/* Observações logísticas */}
        {resumo.observacoesLogisticas && (
          <div className="mt-6">
            <h3 className="text-sm font-semibold tracking-wide text-zinc-200">
              Observações Logísticas
            </h3>
            <p className="mt-2 rounded-xl border border-zinc-800/60 bg-zinc-950/40 px-4 py-3 text-sm whitespace-pre-line text-zinc-300">
              {resumo.observacoesLogisticas}
            </p>
          </div>
        )}

        {/* Totais */}
        <div className="mt-6 space-y-2 border-t border-zinc-800/60 pt-4">
          <div className="flex items-center justify-between text-sm">
            <span className="flex items-center gap-2 text-zinc-400">
              <Clock className="h-4 w-4" aria-hidden="true" />
              Duração total
            </span>
            <span data-testid="revisao-duracao" className="font-medium tabular-nums text-zinc-100">
              {formatarDuracao(resumo.duracaoTotalMin)}
            </span>
          </div>
          <div className="flex items-center justify-between text-sm">
            <span className="flex items-center gap-2 text-zinc-400">
              <Receipt className="h-4 w-4" aria-hidden="true" />
              Valor total
            </span>
            <span
              data-testid="revisao-valor"
              className="text-base font-bold tabular-nums text-red-400"
            >
              {formatarReais(resumo.valorTotal)}
            </span>
          </div>
        </div>

        {/* Ações */}
        <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:items-center sm:justify-end">
          <Button
            type="button"
            variant="outline"
            onClick={onEditar}
            disabled={confirmando}
            className="h-10 rounded-full border-zinc-700/60 bg-transparent px-5 text-sm text-zinc-300 hover:bg-zinc-800/50 hover:text-zinc-100"
          >
            <Pencil className="mr-2 h-4 w-4" aria-hidden="true" />
            Editar
          </Button>
          <Button
            type="button"
            onClick={onConfirmar}
            disabled={confirmando}
            className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 disabled:opacity-60"
          >
            {confirmando ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden="true" />
                Confirmando…
              </>
            ) : (
              <>
                <Check className="mr-2 h-4 w-4" aria-hidden="true" />
                Confirmar agendamento
              </>
            )}
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

interface CampoRevisaoProps {
  icone: ReactNode;
  rotulo: string;
  children: ReactNode;
}

/** Linha rótulo/valor do resumo, com ícone consistente com as demais telas. */
function CampoRevisao({ icone, rotulo, children }: CampoRevisaoProps) {
  return (
    <div className="rounded-xl border border-zinc-800/60 bg-zinc-950/40 px-4 py-3">
      <dt className="flex items-center gap-2 text-xs font-medium tracking-wide text-zinc-500 uppercase">
        <span className="text-red-500">{icone}</span>
        {rotulo}
      </dt>
      <dd className="mt-1 text-sm text-zinc-200">{children}</dd>
    </div>
  );
}
