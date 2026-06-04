import { AxiosError } from 'axios';
import { AlertCircle, Check, X } from 'lucide-react';
import { useCallback, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { Separator } from '@/components/ui/separator';
import { useFiliais } from '@/hooks/useAgendamentoQueries';
import { agendamentoService } from '@/services/agendamentoService';

import { AgendamentoPageHeader } from './AgendamentoPageHeader';
import { AgendamentoStepper } from './AgendamentoStepper';
import { ClienteVeiculoStep } from './ClienteVeiculoStep';
import { ResumoConfirmacaoStep } from './ResumoConfirmacaoStep';
import { ServicosStep } from './ServicosStep';

import type {
  AgendamentoWizardState,
  ClienteResumido,
  CriarAgendamentoPayload,
  ServicoAtivo,
  VeiculoResumido,
} from '@/types/agendamento';
import type { ProblemDetails } from '@/types/auth';

const API_MESSAGES: Record<number, string> = {
  400: 'Selecione uma filial válida para prosseguir.',
  401: 'Sessao expirada. Faca login novamente.',
  403: 'Voce nao possui permissao para criar agendamentos.',
  404: 'Filial não encontrada.',
  409: 'Conflito de horário detectado. Ajuste os dados e tente novamente.',
  500: 'Nao foi possivel concluir o agendamento no momento. Tente novamente.',
};

const INITIAL_STATE: AgendamentoWizardState = {
  filialId: '',
  filialNome: '',
  cliente: null,
  veiculo: null,
  dataAgendamento: '',
  horaInicio: '',
  servicos: [],
};

export function NovoAgendamentoPage() {
  const navigate = useNavigate();

  const [currentStep, setCurrentStep] = useState(1);

  const [wizardState, setWizardState] = useState<AgendamentoWizardState>(INITIAL_STATE);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [confirmado, setConfirmado] = useState(false);

  const [globalError, setGlobalError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  // RF019 — carrega filiais ativas para o seletor.
  const {
    data: filiaisData,
    isLoading: filiaisCarregando,
    isError: filiaisErro,
    refetch: refetchFiliais,
  } = useFiliais();
  const filiais = useMemo(() => filiaisData?.itens ?? [], [filiaisData]);

  const handleFilialChange = useCallback((filialId: string, filialNome: string) => {
    setWizardState((prev) => ({ ...prev, filialId, filialNome }));
    setGlobalError(null);
  }, []);

  const handleClienteChange = useCallback((cliente: ClienteResumido | null) => {
    setWizardState((prev) => ({ ...prev, cliente }));
    setGlobalError(null);
  }, []);

  const handleVeiculoChange = useCallback((veiculo: VeiculoResumido | null) => {
    setWizardState((prev) => ({ ...prev, veiculo }));
    setGlobalError(null);
  }, []);

  const handleDataChange = useCallback((dataAgendamento: string) => {
    setWizardState((prev) => ({ ...prev, dataAgendamento }));
    setGlobalError(null);
  }, []);

  const handleHoraChange = useCallback((horaInicio: string) => {
    setWizardState((prev) => ({ ...prev, horaInicio }));
    setGlobalError(null);
  }, []);

  const handleServicosChange = useCallback((servicos: ServicoAtivo[]) => {
    setWizardState((prev) => ({ ...prev, servicos }));
    setGlobalError(null);
  }, []);

  const goToStep = useCallback((step: number) => {
    setCurrentStep(step);
    setGlobalError(null);
  }, []);

  const handleCancel = useCallback(() => {
    setWizardState(INITIAL_STATE);
    setGlobalError(null);
    setSuccessMsg(null);
    setConfirmado(false);
    void navigate('/dashboard', { replace: true });
  }, [navigate]);

  const handleConfirm = useCallback(async () => {
    if (isSubmitting) return;
    if (!confirmado) return;

    const { cliente, veiculo, dataAgendamento, horaInicio, servicos } = wizardState;

    if (!cliente || !veiculo || !dataAgendamento || !horaInicio || servicos.length === 0) {
      setGlobalError(
        'Existem campos obrigatorios nao preenchidos. Revise as etapas e tente novamente.',
      );
      return;
    }

    // RF019 — filialId é obrigatório.
    if (!wizardState.filialId) {
      setGlobalError('Selecione uma filial para prosseguir.');
      return;
    }

    // RF019 — revalidar que a filial selecionada ainda existe na lista atual.
    const filialAindaValida = filiais.some((f) => f.id === wizardState.filialId && f.ativo);
    if (filiais.length > 0 && !filialAindaValida) {
      setWizardState((prev) => ({ ...prev, filialId: '', filialNome: '' }));
      setGlobalError('A filial selecionada não está mais disponível. Selecione outra filial.');
      return;
    }

    const inicio = new Date(`${dataAgendamento}T${horaInicio}:00`);

    const payload: CriarAgendamentoPayload = {
      clienteId: cliente.id,
      veiculoId: veiculo.id,
      filialId: wizardState.filialId,
      responsavelId: 'mock-responsavel-id',
      inicio: inicio.toISOString(),
      servicoIds: servicos.map((s) => s.id),
    };

    setGlobalError(null);
    setSuccessMsg(null);
    setIsSubmitting(true);

    try {
      await agendamentoService.criarAgendamento(payload);
      setSuccessMsg('Agendamento criado com sucesso!');

      setTimeout(() => {
        void navigate('/dashboard', { replace: true });
      }, 1200);
    } catch (error) {
      if (!(error instanceof AxiosError) || !error.response) {
        // Erro de rede ou erro genérico — NÃO apagar dados do formulário.
        setGlobalError('Não foi possível concluir o agendamento no momento. Tente novamente.');
        return;
      }

      const status = error.response.status;
      const problem = error.response.data as ProblemDetails | undefined;

      if (status === 409) {
        // RF008.3 — Diferenciar conflito de capacidade vs conflito de veículo.
        const detail = (problem?.detail ?? '').toLowerCase();
        const title = (problem?.title ?? '').toLowerCase();
        const texto = detail || title;

        if (texto.includes('capacidade') || texto.includes('filial')) {
          setGlobalError('Capacidade da filial atingida para o horário informado.');
        } else if (texto.includes('veículo') || texto.includes('veiculo')) {
          setGlobalError('Já existe agendamento para este veículo no horário informado.');
        } else {
          // Fallback: usa título do backend se reconhecível, senão mensagem genérica.
          setGlobalError(
            problem?.title ?? 'Conflito de horário detectado. Ajuste os dados e tente novamente.',
          );
        }
        // NÃO limpar formulário, NÃO redirecionar — o usuário pode ajustar e reenviar.
        return;
      }

      if (status === 400 && problem?.title) {
        setGlobalError(problem.title);
        return;
      }

      if (status === 500) {
        // RF008.3 — Erro de servidor: mensagem genérica, sem apagar dados.
        setGlobalError('Não foi possível concluir o agendamento no momento. Tente novamente.');
        return;
      }

      setGlobalError(
        API_MESSAGES[status] ??
          'Não foi possível concluir o agendamento no momento. Tente novamente.',
      );
    } finally {
      setIsSubmitting(false);
    }
  }, [isSubmitting, confirmado, wizardState, navigate, filiais]);

  return (
    <>
      <AgendamentoPageHeader step={currentStep} onCancel={handleCancel} />

      <div className="grid grid-cols-[minmax(240px,300px)_minmax(0,1fr)] gap-6 px-8">
        <AgendamentoStepper currentStep={currentStep} />

        <div className="rounded-2xl border border-zinc-800/60 bg-zinc-900/30 p-8">
          {globalError && (
            <div
              role="alert"
              className="mb-6 flex items-start gap-3 rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3"
            >
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-red-500" />
              <div className="flex-1">
                <p className="text-sm font-medium text-red-400">{globalError}</p>
              </div>
              <button
                type="button"
                onClick={() => setGlobalError(null)}
                className="shrink-0 text-red-500/60 transition-colors hover:text-red-400"
                aria-label="Fechar mensagem de erro"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          )}

          {successMsg && (
            <div
              role="status"
              className="mb-6 flex items-start gap-3 rounded-xl border border-green-500/30 bg-green-950/30 px-4 py-3"
            >
              <Check className="mt-0.5 h-4 w-4 shrink-0 text-green-500" />
              <p className="flex-1 text-sm font-medium text-green-400">{successMsg}</p>
              <button
                type="button"
                onClick={() => setSuccessMsg(null)}
                className="shrink-0 text-green-500/60 transition-colors hover:text-green-400"
                aria-label="Fechar mensagem de sucesso"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          )}

          {currentStep === 1 && (
            <ClienteVeiculoStep
              filialId={wizardState.filialId}
              onFilialChange={handleFilialChange}
              filiais={filiais}
              filiaisCarregando={filiaisCarregando}
              filiaisErro={filiaisErro}
              onRetryFiliais={() => {
                void refetchFiliais();
              }}
              cliente={wizardState.cliente}
              veiculo={wizardState.veiculo}
              dataAgendamento={wizardState.dataAgendamento}
              horaInicio={wizardState.horaInicio}
              onClienteChange={handleClienteChange}
              onVeiculoChange={handleVeiculoChange}
              onDataChange={handleDataChange}
              onHoraChange={handleHoraChange}
              onNext={() => goToStep(2)}
            />
          )}

          {currentStep === 2 && (
            <>
              <Separator className="mb-6 bg-zinc-800/50" />
              <ServicosStep
                servicosSelecionados={wizardState.servicos}
                onServicosChange={handleServicosChange}
                onNext={() => goToStep(3)}
                onBack={() => goToStep(1)}
              />
            </>
          )}

          {currentStep === 3 && (
            <>
              <Separator className="mb-6 bg-zinc-800/50" />
              <ResumoConfirmacaoStep
                wizardState={wizardState}
                isSubmitting={isSubmitting}
                confirmado={confirmado}
                onConfirmadoChange={setConfirmado}
                onConfirm={handleConfirm}
                onBack={() => goToStep(2)}
              />
            </>
          )}
        </div>
      </div>
    </>
  );
}
