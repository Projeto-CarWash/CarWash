import { AxiosError } from 'axios';
import { AlertCircle, Check, X } from 'lucide-react';
import { useCallback, useEffect, useMemo, useState } from 'react';
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
  401: 'Sessão expirada. Faça login novamente.',
  403: 'Você não possui permissão para realizar esta operação.',
  404: 'Filial não encontrada.',
  409: 'Conflito detectado. Ajuste os dados e tente novamente.',
  500: 'Não foi possível concluir o agendamento no momento. Tente novamente.',
};

const INITIAL_STATE: AgendamentoWizardState = {
  filialId: '',
  filialNome: '',
  cliente: null,
  veiculo: null,
  dataAgendamento: '',
  horaInicio: '',
  servicos: [],
  observacoesLogisticas: '',
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

  const handleObservacoesLogisticasChange = useCallback((observacoesLogisticas: string) => {
    setWizardState((prev) => ({ ...prev, observacoesLogisticas }));
  }, []);

  const [obsLogisticasErro, setObsLogisticasErro] = useState<string | null>(null);

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

    // RF019/RF024 — payload real, sem valores mockados. `responsavelId` é
    // opcional e omitido enquanto o fluxo não tem seleção de responsável.
    const payload: CriarAgendamentoPayload = {
      clienteId: cliente.id,
      veiculoId: veiculo.id,
      filialId: wizardState.filialId,
      inicio: inicio.toISOString(),
      servicoIds: servicos.map((s) => s.id),
    };

    // Inclui observações logísticas apenas quando informadas (trim no envio).
    const obsLogisticas = wizardState.observacoesLogisticas?.trim();
    if (obsLogisticas) {
      payload.observacoesLogisticas = obsLogisticas;
    }

    setGlobalError(null);
    setSuccessMsg(null);
    setIsSubmitting(true);

    try {
      await agendamentoService.criarAgendamento(payload);
      setSuccessMsg('Agendamento salvo com sucesso.');
      setObsLogisticasErro(null);

      setTimeout(() => {
        void navigate('/dashboard', { replace: true });
      }, 1200);
    } catch (error) {
      // 401 (sessão expirada) é tratado pelo interceptor do axios: tenta refresh
      // e, em falha, redireciona para /login (fluxo padrão de autenticação).
      if (!(error instanceof AxiosError) || !error.response) {
        // Erro de rede ou erro genérico — NÃO apagar dados do formulário.
        setGlobalError('Não foi possível concluir o agendamento no momento. Tente novamente.');
        return;
      }

      const status = error.response.status;
      const problem = error.response.data as ProblemDetails | undefined;

      // 404 (filial não encontrada): a seleção não é mais válida — limpa o campo,
      // volta à etapa 1 e pede nova escolha.
      if (status === 404) {
        setWizardState((prev) => ({ ...prev, filialId: '', filialNome: '' }));
        setConfirmado(false);
        setGlobalError(API_MESSAGES[404]!);
        goToStep(1);
        return;
      }

      if (status === 409) {
        // Diferencia os motivos de 409: filial inativa (RF019) vs conflito de
        // capacidade ou de veículo (RF008.3).
        const detail = (problem?.detail ?? '').toLowerCase();
        const title = (problem?.title ?? '').toLowerCase();
        const texto = detail || title;

        if (texto.includes('inativa')) {
          // Filial inativa: a seleção deixou de ser válida — volta à etapa 1.
          setWizardState((prev) => ({ ...prev, filialId: '', filialNome: '' }));
          setConfirmado(false);
          setGlobalError(
            'A filial selecionada está inativa e não pode receber novos agendamentos.',
          );
          goToStep(1);
        } else if (texto.includes('capacidade')) {
          setGlobalError('Capacidade da filial atingida para o horário informado.');
        } else if (texto.includes('veículo') || texto.includes('veiculo')) {
          setGlobalError('Já existe agendamento para este veículo no horário informado.');
        } else {
          // Fallback: usa título do backend se reconhecível, senão mensagem genérica.
          setGlobalError(
            problem?.title ?? 'Conflito detectado. Ajuste os dados e tente novamente.',
          );
        }
        // Em conflito de capacidade/veículo NÃO limpa o formulário — o usuário ajusta e reenvia.
        return;
      }

      // 400: prioriza o erro por campo de `filialId` quando o backend o devolve;
      // caso contrário, usa a mensagem padrão associada ao campo Filial.
      if (status === 400) {
        const erroFilial = problem?.errors?.filialId?.[0] ?? problem?.errors?.FilialId?.[0];
        const erroObsLog =
          problem?.errors?.observacoesLogisticas?.[0] ??
          problem?.errors?.ObservacoesLogisticas?.[0];
        if (erroObsLog) {
          setObsLogisticasErro('Dados da observação inválidos. Verifique e tente novamente.');
        }
        setGlobalError(erroFilial ?? problem?.title ?? API_MESSAGES[400]!);
        return;
      }

      // 409: agendamento no estado atual não permite edição da observação.
      if (status === 409) {
        setObsLogisticasErro('A observação não pode ser alterada no estado atual do agendamento.');
        setGlobalError(API_MESSAGES[status] ?? API_MESSAGES[500]!);
        return;
      }

      // 403: sem permissão para gerenciar observações logísticas.
      if (status === 403) {
        setGlobalError('Você não possui permissão para gerenciar observações logísticas.');
        return;
      }

      // 500 e demais: preserva todos os dados preenchidos e permite nova tentativa.
      setGlobalError(API_MESSAGES[status] ?? API_MESSAGES[500]!);
    } finally {
      setIsSubmitting(false);
    }
  }, [isSubmitting, confirmado, wizardState, navigate, filiais, goToStep]);

  // RF019 — integridade da seleção: se a lista de filiais for recarregada e a
  // filial escolhida não existir mais (ou ficar inativa), limpa a seleção
  // inválida e solicita nova escolha, bloqueando a finalização.
  useEffect(() => {
    if (!wizardState.filialId || filiais.length === 0) return;

    const aindaValida = filiais.some((f) => f.id === wizardState.filialId && f.ativo);
    if (!aindaValida) {
      // Sincroniza o estado do formulário com a lista recarregada (fonte externa):
      // a seleção deixou de ser válida e precisa ser limpa.
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setWizardState((prev) => ({ ...prev, filialId: '', filialNome: '' }));
      setConfirmado(false);
      setGlobalError('A filial selecionada não está mais disponível. Selecione outra filial.');
    }
  }, [filiais, wizardState.filialId]);

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
                observacoesLogisticas={wizardState.observacoesLogisticas ?? ''}
                onObservacoesLogisticasChange={handleObservacoesLogisticasChange}
                observacoesLogisticasErro={obsLogisticasErro ?? undefined}
              />
            </>
          )}
        </div>
      </div>
    </>
  );
}
