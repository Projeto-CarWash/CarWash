import {
  AlertCircle,
  Building2,
  Car,
  ChevronLeft,
  Clock,
  DollarSign,
  Loader2,
  User,
  Wrench,
} from 'lucide-react';
import * as React from 'react';

import { Button } from '@/components/ui/button';

import { ObservacoesLogisticasField } from './ObservacoesLogisticasField';

import type { AgendamentoWizardState } from '@/types/agendamento';

function formatarPreco(valor: number): string {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

function formatarDuracao(minutos: number): string {
  if (minutos < 60) return `${minutos} min`;
  const h = Math.floor(minutos / 60);
  const m = minutos % 60;
  return m > 0 ? `${h}h ${m}min` : `${h}h`;
}

function formatarData(iso: string): string {
  const [y, m, d] = iso.split('-');
  return `${d}/${m}/${y}`;
}

function formatarDoc(cpf?: string, cnpj?: string): string {
  if (cpf?.length === 11) {
    return `${cpf.slice(0, 3)}.${cpf.slice(3, 6)}.${cpf.slice(6, 9)}-${cpf.slice(9)}`;
  }
  if (cnpj?.length === 14) {
    return `${cnpj.slice(0, 2)}.${cnpj.slice(2, 5)}.${cnpj.slice(5, 8)}/${cnpj.slice(8, 12)}-${cnpj.slice(12)}`;
  }
  return '';
}

function calcularHoraFim(horaInicio: string, duracaoMinutos: number): string {
  const [h, m] = horaInicio.split(':').map(Number);
  const totalMinutos = Number(h) * 60 + Number(m) + duracaoMinutos;
  const fh = Math.floor(totalMinutos / 60) % 24;
  const fm = totalMinutos % 60;
  return `${String(fh).padStart(2, '0')}:${String(fm).padStart(2, '0')}`;
}

interface ResumoConfirmacaoStepProps {
  wizardState: AgendamentoWizardState;
  isSubmitting: boolean;
  onConfirm: () => void;
  onBack: () => void;
  confirmado: boolean;
  onConfirmadoChange: (checked: boolean) => void;
  /** Valor atual das observações logísticas (controlado pelo pai). */
  observacoesLogisticas: string;
  /** Callback para atualizar as observações logísticas no wizard state. */
  onObservacoesLogisticasChange: (value: string) => void;
  /** Erro de validação do campo (de Zod ou do backend). */
  observacoesLogisticasErro?: string;
  /** Conflito de veículo do RF020. */
  conflitoVeiculo?: boolean;
}

export function ResumoConfirmacaoStep({
  wizardState,
  isSubmitting,
  onConfirm,
  onBack,
  confirmado,
  onConfirmadoChange,
  observacoesLogisticas,
  onObservacoesLogisticasChange,
  observacoesLogisticasErro,
  conflitoVeiculo,
}: ResumoConfirmacaoStepProps) {
  const { cliente, veiculo, servicos, dataAgendamento, horaInicio, filialNome } = wizardState;

  const duracaoTotal = servicos.reduce((sum, s) => sum + s.duracao, 0);
  const valorTotal = servicos.reduce((sum, s) => sum + s.preco, 0);
  const horaFim = calcularHoraFim(horaInicio, duracaoTotal);

  const errorContainerRef = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    if (conflitoVeiculo && errorContainerRef.current) {
      errorContainerRef.current.focus();
    }
  }, [conflitoVeiculo]);

  return (
    <div>
      <div className="mb-5">
        <h3 className="text-xl font-semibold text-foreground">Resumo do Agendamento</h3>
        <p className="mt-1 text-sm text-muted-foreground">
          Confirme os dados abaixo para concluir o agendamento.
        </p>
      </div>

      <div className="space-y-4">
        <div className="rounded-xl border border-border bg-card p-4">
          <p className="mb-3 text-[10px] font-bold tracking-[0.2em] text-muted-foreground">FILIAL</p>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-red-600/10">
              <Building2 className="h-4.5 w-4.5 text-red-500" />
            </div>
            <div>
              <p className="text-sm font-medium text-foreground">{filialNome || 'Não selecionada'}</p>
            </div>
          </div>
        </div>
        <div className="rounded-xl border border-border bg-card p-4">
          <p className="mb-3 text-[10px] font-bold tracking-[0.2em] text-muted-foreground">CLIENTE</p>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-red-600/10">
              <User className="h-4.5 w-4.5 text-red-500" />
            </div>
            <div>
              <p className="text-sm font-medium text-foreground">{cliente?.nome}</p>
              <p className="text-xs text-muted-foreground">{formatarDoc(cliente?.cpf, cliente?.cnpj)}</p>
            </div>
          </div>
        </div>

        <div
          className={`rounded-xl border ${conflitoVeiculo ? 'border-red-500 bg-red-950/20' : 'border-border bg-card'} p-4 outline-none`}
          ref={errorContainerRef}
          tabIndex={-1}
          aria-invalid={conflitoVeiculo ? 'true' : undefined}
        >
          {conflitoVeiculo && (
            <div
              role="alert"
              aria-live="assertive"
              className="mb-4 flex items-start gap-3 rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3"
            >
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-red-500" aria-hidden="true" />
              <div className="flex-1 space-y-1">
                <p className="text-sm font-medium text-red-400">
                  Veículo já possui agendamento neste horário.
                </p>
                <p className="text-xs text-red-400/80">
                  Esse bloqueio vale para toda a operação, inclusive entre filiais.
                </p>
              </div>
            </div>
          )}

          <p className="mb-3 text-[10px] font-bold tracking-[0.2em] text-muted-foreground">VEÍCULO</p>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-red-600/10">
              <Car className="h-4.5 w-4.5 text-red-500" />
            </div>
            <div>
              <p className="text-sm font-medium text-foreground">{veiculo?.modelo}</p>
              <p className="text-xs text-muted-foreground">
                {veiculo?.placa} · {veiculo?.cor}
                {veiculo?.ano ? ` · ${veiculo.ano}` : ''}
              </p>
            </div>
          </div>
        </div>

        {wizardState.responsavel && (
          <div className="rounded-xl border border-border bg-card p-4">
            <p className="mb-3 text-[10px] font-bold tracking-[0.2em] text-muted-foreground">RESPONSÁVEL</p>
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-full bg-red-600/10">
                <User className="h-4.5 w-4.5 text-red-500" />
              </div>
              <div>
                <p className="text-sm font-medium text-foreground">
                  {wizardState.responsavel.nome}
                  {wizardState.responsavel.documento && (
                    <span className="ml-1.5 text-xs font-normal text-muted-foreground">
                      (
                      {formatarDoc(
                        wizardState.responsavel.documento.replace(/\D/g, '').length === 11
                          ? wizardState.responsavel.documento.replace(/\D/g, '')
                          : undefined,
                        wizardState.responsavel.documento.replace(/\D/g, '').length === 14
                          ? wizardState.responsavel.documento.replace(/\D/g, '')
                          : undefined,
                      )}
                      )
                    </span>
                  )}
                </p>
                <p className="text-xs text-muted-foreground">Responsável pelo agendamento</p>
              </div>
            </div>
          </div>
        )}

        <div
          className={`rounded-xl border ${conflitoVeiculo ? 'border-red-500 bg-red-950/20' : 'border-border bg-card'} p-4`}
          aria-invalid={conflitoVeiculo ? 'true' : undefined}
        >
          <p className="mb-3 text-[10px] font-bold tracking-[0.2em] text-muted-foreground">
            DATA E HORÁRIO
          </p>
          <div className="grid grid-cols-3 gap-4">
            <div>
              <p className="text-xs text-muted-foreground">Data</p>
              <p className="text-sm font-medium text-foreground">{formatarData(dataAgendamento)}</p>
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Início</p>
              <p className="text-sm font-medium text-foreground">{horaInicio}</p>
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Término previsto</p>
              <p className="text-sm font-medium text-foreground">{horaFim}</p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-border bg-card p-4">
          <p className="mb-3 text-[10px] font-bold tracking-[0.2em] text-muted-foreground">
            SERVIÇOS ({servicos.length})
          </p>
          <div className="space-y-2">
            {servicos.map((s) => (
              <div
                key={s.id}
                className="flex items-center justify-between rounded-lg bg-muted px-3 py-2"
              >
                <div className="flex items-center gap-2">
                  <Wrench className="h-3.5 w-3.5 text-muted-foreground" />
                  <span className="text-sm text-foreground">{s.nome}</span>
                </div>
                <div className="flex items-center gap-3 text-xs text-muted-foreground">
                  <span className="flex items-center gap-1">
                    <Clock className="h-3 w-3" />
                    {formatarDuracao(s.duracao)}
                  </span>
                  <span className="font-medium text-foreground">{formatarPreco(s.preco)}</span>
                </div>
              </div>
            ))}
          </div>

          <div className="mt-3 flex items-center justify-between border-t border-border pt-3">
            <div className="flex items-center gap-3 text-sm">
              <span className="flex items-center gap-1 text-muted-foreground">
                <Clock className="h-3.5 w-3.5" />
                Duração total:{' '}
                <span className="font-medium text-foreground">{formatarDuracao(duracaoTotal)}</span>
              </span>
            </div>
            <div className="flex items-center gap-1 text-sm font-semibold text-foreground">
              <DollarSign className="h-4 w-4 text-red-500" />
              {formatarPreco(valorTotal)}
            </div>
          </div>
        </div>

        {/* Observações logísticas */}
        <ObservacoesLogisticasField
          id="resumo-obs-logisticas"
          value={observacoesLogisticas}
          onChange={onObservacoesLogisticasChange}
          error={observacoesLogisticasErro}
          disabled={isSubmitting}
        />

        <label
          htmlFor="confirmacao"
          className="flex cursor-pointer items-start gap-3 rounded-xl border border-border bg-card px-4 py-3 transition-colors hover:bg-accent"
        >
          <input
            id="confirmacao"
            type="checkbox"
            checked={confirmado}
            onChange={(e) => onConfirmadoChange(e.target.checked)}
            disabled={isSubmitting}
            className="mt-0.5 h-4 w-4 shrink-0 cursor-pointer rounded border-border bg-muted text-red-600 focus:ring-red-600/30 focus:ring-offset-0"
          />
          <span className="text-sm text-foreground">
            Li e confirmo os dados do agendamento. Desejo prosseguir com a criação.
          </span>
        </label>
      </div>

      <div className="mt-8 flex items-center justify-between">
        <Button
          type="button"
          variant="outline"
          onClick={onBack}
          disabled={isSubmitting}
          className="h-10 rounded-full border-border bg-transparent px-5 text-sm text-muted-foreground hover:bg-accent hover:text-foreground"
        >
          <ChevronLeft className="mr-1 h-4 w-4" />
          Voltar
        </Button>
        <Button
          type="button"
          onClick={onConfirm}
          disabled={!confirmado || isSubmitting}
          className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 disabled:opacity-50"
        >
          {isSubmitting ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Agendando…
            </>
          ) : (
            'Confirmar agendamento'
          )}
        </Button>
      </div>
    </div>
  );
}
