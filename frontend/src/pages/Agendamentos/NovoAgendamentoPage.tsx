import { zodResolver } from '@hookform/resolvers/zod';
import { AlertCircle, ArrowLeft, CalendarPlus, Check, Loader2, X } from 'lucide-react';
import { useCallback, useMemo, useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  useClientesParaAgendamento,
  useConfirmarAgendamento,
  useFiliais,
  usePreConfirmarAgendamento,
  useServicos,
  useVeiculosDoCliente,
} from '@/hooks/useAgendamentoQueries';
import { tratarErroApi } from '@/lib/apiError';
import { agendamentoSchema, type AgendamentoFormData } from '@/schemas/agendamentoSchema';

import { ResumoAgendamento } from './ResumoAgendamento';
import { RevisaoAgendamento } from './RevisaoAgendamento';
import { SeletorServicos } from './SeletorServicos';

import type {
  ConfirmarAgendamentoRequest,
  CriarAgendamentoRequest,
  PreConfirmacaoResponse,
} from '@/types/agendamento';

/** Etapas do fluxo de criação com confirmação (RF015). */
type Etapa = 'edicao' | 'revisao';

/**
 * Converte um valor de `<input type="datetime-local">` (hora local, sem fuso)
 * para ISO-8601 UTC com sufixo `Z`, conforme exigido pelo contrato da API.
 */
function paraIsoUtc(datetimeLocal: string): string {
  return new Date(datetimeLocal).toISOString();
}

/** Normaliza os dados do formulário para o corpo dos endpoints de agendamento. */
function montarPayload(data: AgendamentoFormData): CriarAgendamentoRequest {
  return {
    filialId: data.filialId,
    clienteId: data.clienteId,
    veiculoId: data.veiculoId,
    responsavelId: data.responsavelId ? data.responsavelId : null,
    inicio: paraIsoUtc(data.inicio),
    servicoIds: data.servicoIds,
    observacoes: data.observacoes?.trim() ? data.observacoes.trim() : null,
  };
}

/** Mapeia nomes de campo retornados pelo backend (400) para campos do form. */
const MAPA_CAMPO_BACKEND: Record<string, keyof AgendamentoFormData> = {
  filialid: 'filialId',
  clienteid: 'clienteId',
  veiculoid: 'veiculoId',
  responsavelid: 'responsavelId',
  inicio: 'inicio',
  servicoids: 'servicoIds',
  observacoes: 'observacoes',
};

/**
 * Tela de criação de agendamento com confirmação em 2 etapas (RF007 + RF015).
 *
 * <p>Etapa `edicao`: o formulário submete em `POST /agendamentos/pre-confirmacao`
 * (gera resumo + token, não persiste). Etapa `revisao`: exibe
 * `RevisaoAgendamento` e, na confirmação explícita, chama
 * `POST /agendamentos/confirmar` (persiste). A revisão é uma etapa da MESMA
 * página — sem rota nova — para preservar o formulário preenchido.</p>
 */
export function NovoAgendamentoPage() {
  const navigate = useNavigate();
  const [globalError, setGlobalError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [buscaCliente, setBuscaCliente] = useState('');

  // Estado do fluxo de confirmação (RF015).
  const [etapa, setEtapa] = useState<Etapa>('edicao');
  const [preConfirmacao, setPreConfirmacao] = useState<PreConfirmacaoResponse | null>(null);
  const [idempotencyKey, setIdempotencyKey] = useState<string | null>(null);
  const [erroRevisao, setErroRevisao] = useState<string | null>(null);

  const form = useForm<AgendamentoFormData>({
    resolver: zodResolver(agendamentoSchema),
    mode: 'onBlur',
    shouldFocusError: true,
    defaultValues: {
      filialId: '',
      clienteId: '',
      veiculoId: '',
      responsavelId: '',
      inicio: '',
      servicoIds: [],
      observacoes: '',
    },
  });

  const { errors } = form.formState;

  const clienteId = useWatch({ control: form.control, name: 'clienteId' });
  const servicoIds = useWatch({ control: form.control, name: 'servicoIds' });
  const observacoes = useWatch({ control: form.control, name: 'observacoes' }) ?? '';

  const clientesQuery = useClientesParaAgendamento(buscaCliente);
  const filiaisQuery = useFiliais();
  const servicosQuery = useServicos();
  const veiculosQuery = useVeiculosDoCliente(clienteId || undefined);

  const preConfirmarMutation = usePreConfirmarAgendamento();
  const confirmarMutation = useConfirmarAgendamento();

  const servicos = useMemo(() => servicosQuery.data?.itens ?? [], [servicosQuery.data]);
  const servicosSelecionados = useMemo(
    () => servicos.filter((s) => servicoIds.includes(s.id)),
    [servicos, servicoIds],
  );

  const handleToggleServico = useCallback(
    (servicoId: string) => {
      const atuais = form.getValues('servicoIds');
      const proximos = atuais.includes(servicoId)
        ? atuais.filter((id) => id !== servicoId)
        : [...atuais, servicoId];
      form.setValue('servicoIds', proximos, { shouldValidate: true, shouldDirty: true });
    },
    [form],
  );

  /** Aplica os erros de campo retornados pelo backend (400) e foca o primeiro. */
  const aplicarErrosDeCampo = useCallback(
    (errorsPorCampo: Record<string, string>) => {
      let focou = false;
      for (const [campo, mensagem] of Object.entries(errorsPorCampo)) {
        const chave = MAPA_CAMPO_BACKEND[campo.toLowerCase()];
        if (chave) {
          form.setError(chave, { message: mensagem });
          if (!focou) {
            form.setFocus(chave);
            focou = true;
          }
        }
      }
    },
    [form],
  );

  /**
   * Submit do formulário (etapa de edição): solicita a pré-confirmação. Em
   * sucesso, guarda a resposta, gera uma `idempotencyKey` estável e avança
   * para a etapa de revisão.
   */
  const onSubmit = useCallback(
    async (data: AgendamentoFormData) => {
      setGlobalError(null);
      setSuccessMsg(null);

      try {
        const resp = await preConfirmarMutation.mutateAsync(montarPayload(data));
        setPreConfirmacao(resp);
        // Uma idempotencyKey por sessão de revisão — só regenerada ao refazer.
        setIdempotencyKey(crypto.randomUUID());
        setErroRevisao(null);
        setEtapa('revisao');
      } catch (error) {
        const info = tratarErroApi(error);
        setGlobalError(info.mensagem);
        aplicarErrosDeCampo(info.errorsPorCampo);

        // 409: conflito de agenda do veículo (RN011) já detectado na prévia.
        if (info.status === 409) {
          form.setError('veiculoId', { message: 'Veículo com conflito de agenda neste horário.' });
          form.setError('inicio', { message: 'Escolha outro horário para este veículo.' });
        }
      }
    },
    [aplicarErrosDeCampo, form, preConfirmarMutation],
  );

  /** Volta da revisão para a edição preservando o formulário. */
  const voltarParaEdicao = useCallback((aviso?: string) => {
    setEtapa('edicao');
    setPreConfirmacao(null);
    setIdempotencyKey(null);
    setErroRevisao(null);
    setGlobalError(aviso ?? null);
  }, []);

  /**
   * Confirmação explícita (etapa de revisão): persiste o agendamento.
   *
   * <p>Trata os erros específicos do RF015: 410 (sessão expirada) e 409 de
   * divergência voltam para a edição; 409 de conflito permanece na revisão
   * com a mensagem; 400 destaca os campos no formulário.</p>
   */
  const onConfirmar = useCallback(async () => {
    if (!preConfirmacao || !idempotencyKey) {
      return;
    }
    setErroRevisao(null);

    const payload: ConfirmarAgendamentoRequest = {
      ...montarPayload(form.getValues()),
      confirmar: true,
      tokenConfirmacao: preConfirmacao.tokenConfirmacao,
      idempotencyKey,
    };

    try {
      const resp = await confirmarMutation.mutateAsync(payload);
      setEtapa('edicao');
      setPreConfirmacao(null);
      setIdempotencyKey(null);
      setSuccessMsg(resp.mensagem || 'Agendamento criado com sucesso! Redirecionando…');
      form.reset();
      setTimeout(() => {
        void navigate('/dashboard', { replace: true });
      }, 1200);
    } catch (error) {
      const info = tratarErroApi(error);

      // 410: token/sessão expirada — refaça a pré-confirmação.
      if (info.status === 410) {
        voltarParaEdicao(
          'Sessão de confirmação expirada. Gere uma nova pré-confirmação revisando os dados.',
        );
        return;
      }

      // 409: pode ser conflito de agenda OU divergência do resumo. A divergência
      // exige nova revisão; o conflito é exibido na própria etapa de revisão.
      if (info.status === 409) {
        const mensagem = info.mensagem;
        if (/alterad|divergen|revise/i.test(mensagem)) {
          voltarParaEdicao(mensagem);
        } else {
          setErroRevisao(mensagem);
        }
        return;
      }

      // 400: validação por campo — volta para a edição destacando os campos.
      if (info.status === 400 && Object.keys(info.errorsPorCampo).length > 0) {
        aplicarErrosDeCampo(info.errorsPorCampo);
        voltarParaEdicao(info.mensagem);
        return;
      }

      setErroRevisao(info.mensagem);
    }
  }, [
    aplicarErrosDeCampo,
    confirmarMutation,
    form,
    idempotencyKey,
    navigate,
    preConfirmacao,
    voltarParaEdicao,
  ]);

  const isPreConfirmando = form.formState.isSubmitting || preConfirmarMutation.isPending;
  const clientes = clientesQuery.data?.itens ?? [];
  const filiais = filiaisQuery.data?.itens ?? [];
  const veiculos = veiculosQuery.data?.itens ?? [];

  return (
    <div className="px-4 py-6 sm:px-8 sm:py-8">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <span
            className="flex h-10 w-10 items-center justify-center rounded-lg bg-red-600/10 text-red-500"
            aria-hidden="true"
          >
            <CalendarPlus className="h-5 w-5" />
          </span>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-zinc-50">Novo agendamento</h1>
            <p className="mt-1 text-sm text-zinc-400">
              {etapa === 'edicao'
                ? 'Registre um serviço para um veículo em uma filial (RF007).'
                : 'Confirme as informações antes de concluir o agendamento (RF015).'}
            </p>
          </div>
        </div>
        <Button
          type="button"
          variant="outline"
          onClick={() => void navigate(-1)}
          disabled={isPreConfirmando || confirmarMutation.isPending}
          className="h-9 rounded-full border-zinc-700/60 bg-transparent px-4 text-sm text-zinc-300 hover:bg-zinc-800/50 hover:text-zinc-100"
        >
          <ArrowLeft className="mr-1 h-4 w-4" aria-hidden="true" />
          Voltar
        </Button>
      </div>

      {successMsg && (
        <div
          role="status"
          aria-live="polite"
          className="mb-6 flex items-start gap-3 rounded-xl border border-green-500/30 bg-green-950/30 px-4 py-3"
        >
          <Check className="mt-0.5 h-4 w-4 shrink-0 text-green-500" aria-hidden="true" />
          <p className="flex-1 text-sm font-medium text-green-400">{successMsg}</p>
        </div>
      )}

      {etapa === 'revisao' && preConfirmacao ? (
        <RevisaoAgendamento
          resumo={preConfirmacao.resumo}
          onConfirmar={() => void onConfirmar()}
          onEditar={() => voltarParaEdicao()}
          confirmando={confirmarMutation.isPending}
          erro={erroRevisao}
          onLimparErro={() => setErroRevisao(null)}
        />
      ) : (
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(280px,340px)]">
          <Card className="border border-zinc-800/60 bg-zinc-900/30">
            <CardHeader>
              <CardTitle className="text-lg text-zinc-100">Dados do agendamento</CardTitle>
              <CardDescription className="text-zinc-400">
                Filial, veículo e ao menos um serviço são obrigatórios.
              </CardDescription>
            </CardHeader>
            <CardContent>
              {globalError && (
                <div
                  role="alert"
                  aria-live="assertive"
                  className="mb-6 flex items-start gap-3 rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3"
                >
                  <AlertCircle
                    className="mt-0.5 h-4 w-4 shrink-0 text-red-500"
                    aria-hidden="true"
                  />
                  <p className="flex-1 text-sm font-medium text-red-400">{globalError}</p>
                  <button
                    type="button"
                    onClick={() => setGlobalError(null)}
                    aria-label="Fechar mensagem de erro"
                    className="shrink-0 text-red-500/60 transition-colors hover:text-red-400"
                  >
                    <X className="h-4 w-4" aria-hidden="true" />
                  </button>
                </div>
              )}

              <form
                onSubmit={form.handleSubmit(onSubmit)}
                noValidate
                aria-busy={isPreConfirmando}
                className="grid grid-cols-1 gap-5 md:grid-cols-2"
              >
                {/* Cliente */}
                <div className="flex flex-col gap-2 md:col-span-2">
                  <Label htmlFor="ag-cliente" className="text-zinc-300">
                    Cliente
                  </Label>
                  <Input
                    id="ag-cliente-busca"
                    type="search"
                    value={buscaCliente}
                    onChange={(e) => setBuscaCliente(e.target.value)}
                    placeholder="Filtrar clientes por nome, documento…"
                    aria-label="Buscar cliente"
                    className="h-10 rounded-lg border-zinc-700/60 bg-zinc-950/40 px-3 text-sm text-zinc-100 placeholder:text-zinc-500"
                  />
                  <Controller
                    name="clienteId"
                    control={form.control}
                    render={({ field }) => (
                      <select
                        id="ag-cliente"
                        aria-invalid={!!errors.clienteId}
                        aria-describedby={errors.clienteId ? 'ag-cliente-error' : undefined}
                        disabled={clientesQuery.isLoading}
                        className="h-10 rounded-lg border border-zinc-700/60 bg-zinc-950/40 px-3 text-sm text-zinc-100 focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none aria-invalid:border-red-500/60 aria-invalid:ring-3 aria-invalid:ring-red-500/20 disabled:opacity-50"
                        value={field.value}
                        onChange={(e) => {
                          field.onChange(e.target.value);
                          // Trocar de cliente invalida o veículo já escolhido.
                          form.setValue('veiculoId', '', { shouldValidate: false });
                        }}
                        onBlur={field.onBlur}
                      >
                        <option value="">
                          {clientesQuery.isLoading
                            ? 'Carregando clientes…'
                            : 'Selecione um cliente'}
                        </option>
                        {clientes.map((c) => (
                          <option key={c.id} value={c.id}>
                            {c.nome}
                          </option>
                        ))}
                      </select>
                    )}
                  />
                  {clientesQuery.isError && (
                    <p className="text-xs text-amber-400">
                      Não foi possível carregar a lista de clientes.
                    </p>
                  )}
                  {errors.clienteId && (
                    <p id="ag-cliente-error" role="alert" className="text-xs text-red-400">
                      {errors.clienteId.message}
                    </p>
                  )}
                </div>

                {/* Veículo */}
                <div className="flex flex-col gap-2">
                  <Label htmlFor="ag-veiculo" className="text-zinc-300">
                    Veículo
                  </Label>
                  <Controller
                    name="veiculoId"
                    control={form.control}
                    render={({ field }) => (
                      <select
                        id="ag-veiculo"
                        aria-invalid={!!errors.veiculoId}
                        aria-describedby={errors.veiculoId ? 'ag-veiculo-error' : 'ag-veiculo-hint'}
                        disabled={!clienteId || veiculosQuery.isLoading}
                        className="h-10 rounded-lg border border-zinc-700/60 bg-zinc-950/40 px-3 text-sm text-zinc-100 focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none aria-invalid:border-red-500/60 aria-invalid:ring-3 aria-invalid:ring-red-500/20 disabled:opacity-50"
                        value={field.value}
                        onChange={field.onChange}
                        onBlur={field.onBlur}
                      >
                        <option value="">
                          {!clienteId
                            ? 'Selecione um cliente primeiro'
                            : veiculosQuery.isLoading
                              ? 'Carregando veículos…'
                              : 'Selecione um veículo'}
                        </option>
                        {veiculos.map((v) => (
                          <option key={v.id} value={v.id}>
                            {v.placa} — {v.marca} {v.modelo}
                          </option>
                        ))}
                      </select>
                    )}
                  />
                  {veiculosQuery.isError ? (
                    <p id="ag-veiculo-hint" className="text-xs text-amber-400">
                      Catálogo de veículos indisponível — endpoint{' '}
                      <code className="font-mono">GET /api/v1/veiculos</code> pendente no backend.
                    </p>
                  ) : (
                    <p id="ag-veiculo-hint" className="text-xs text-zinc-500">
                      A lista depende do cliente selecionado.
                    </p>
                  )}
                  {errors.veiculoId && (
                    <p id="ag-veiculo-error" role="alert" className="text-xs text-red-400">
                      {errors.veiculoId.message}
                    </p>
                  )}
                </div>

                {/* Filial */}
                <div className="flex flex-col gap-2">
                  <Label htmlFor="ag-filial" className="text-zinc-300">
                    Filial
                  </Label>
                  <Controller
                    name="filialId"
                    control={form.control}
                    render={({ field }) => (
                      <select
                        id="ag-filial"
                        aria-invalid={!!errors.filialId}
                        aria-describedby={errors.filialId ? 'ag-filial-error' : undefined}
                        disabled={filiaisQuery.isLoading}
                        className="h-10 rounded-lg border border-zinc-700/60 bg-zinc-950/40 px-3 text-sm text-zinc-100 focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none aria-invalid:border-red-500/60 aria-invalid:ring-3 aria-invalid:ring-red-500/20 disabled:opacity-50"
                        value={field.value}
                        onChange={field.onChange}
                        onBlur={field.onBlur}
                      >
                        <option value="">
                          {filiaisQuery.isLoading ? 'Carregando filiais…' : 'Selecione uma filial'}
                        </option>
                        {filiais.map((f) => (
                          <option key={f.id} value={f.id}>
                            {f.nome}
                          </option>
                        ))}
                      </select>
                    )}
                  />
                  {filiaisQuery.isError && (
                    <p className="text-xs text-amber-400">
                      Catálogo de filiais indisponível — endpoint{' '}
                      <code className="font-mono">GET /api/v1/filiais</code> pendente no backend.
                    </p>
                  )}
                  {errors.filialId && (
                    <p id="ag-filial-error" role="alert" className="text-xs text-red-400">
                      {errors.filialId.message}
                    </p>
                  )}
                </div>

                {/* Início */}
                <div className="flex flex-col gap-2">
                  <Label htmlFor="ag-inicio" className="text-zinc-300">
                    Data e hora de início
                  </Label>
                  <Input
                    id="ag-inicio"
                    type="datetime-local"
                    aria-invalid={!!errors.inicio}
                    aria-describedby={errors.inicio ? 'ag-inicio-error' : undefined}
                    className="h-10 rounded-lg border-zinc-700/60 bg-zinc-950/40 px-3 text-sm text-zinc-100 [color-scheme:dark]"
                    {...form.register('inicio')}
                  />
                  {errors.inicio && (
                    <p id="ag-inicio-error" role="alert" className="text-xs text-red-400">
                      {errors.inicio.message}
                    </p>
                  )}
                </div>

                {/* Responsável (opcional) */}
                <div className="flex flex-col gap-2">
                  <Label htmlFor="ag-responsavel" className="text-zinc-300">
                    Responsável <span className="text-zinc-500">(opcional)</span>
                  </Label>
                  <Controller
                    name="responsavelId"
                    control={form.control}
                    render={({ field }) => (
                      <select
                        id="ag-responsavel"
                        aria-invalid={!!errors.responsavelId}
                        aria-describedby="ag-responsavel-hint"
                        className="h-10 rounded-lg border border-zinc-700/60 bg-zinc-950/40 px-3 text-sm text-zinc-100 focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none disabled:opacity-50"
                        value={field.value}
                        onChange={field.onChange}
                        onBlur={field.onBlur}
                      >
                        <option value="">Sem responsável definido</option>
                      </select>
                    )}
                  />
                  <p id="ag-responsavel-hint" className="text-xs text-zinc-500">
                    Listagem de responsáveis depende de endpoint ainda não disponível (RF023/RF024).
                  </p>
                  {errors.responsavelId && (
                    <p role="alert" className="text-xs text-red-400">
                      {errors.responsavelId.message}
                    </p>
                  )}
                </div>

                {/* Serviços */}
                <div className="flex flex-col gap-2 md:col-span-2">
                  <Label className="text-zinc-300" id="ag-servicos-label">
                    Serviços
                  </Label>
                  <div aria-labelledby="ag-servicos-label">
                    <SeletorServicos
                      servicos={servicos}
                      selecionados={servicoIds}
                      onToggle={handleToggleServico}
                      carregando={servicosQuery.isLoading}
                      erro={servicosQuery.isError}
                      mensagemErro={errors.servicoIds?.message}
                      disabled={isPreConfirmando}
                    />
                  </div>
                </div>

                {/* Observações */}
                <div className="flex flex-col gap-2 md:col-span-2">
                  <Label htmlFor="ag-observacoes" className="text-zinc-300">
                    Observações <span className="text-zinc-500">(opcional)</span>
                  </Label>
                  <textarea
                    id="ag-observacoes"
                    rows={3}
                    maxLength={500}
                    aria-invalid={!!errors.observacoes}
                    aria-describedby={
                      errors.observacoes ? 'ag-observacoes-error' : 'ag-observacoes-contador'
                    }
                    placeholder="Detalhes adicionais sobre o serviço…"
                    className="rounded-lg border border-zinc-700/60 bg-zinc-950/40 px-3 py-2 text-sm text-zinc-100 placeholder:text-zinc-500 focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none aria-invalid:border-red-500/60 aria-invalid:ring-3 aria-invalid:ring-red-500/20"
                    {...form.register('observacoes')}
                  />
                  {errors.observacoes ? (
                    <p id="ag-observacoes-error" role="alert" className="text-xs text-red-400">
                      {errors.observacoes.message}
                    </p>
                  ) : (
                    <p id="ag-observacoes-contador" className="text-right text-xs text-zinc-500">
                      {observacoes.length}/500
                    </p>
                  )}
                </div>

                {/* Ações */}
                <div className="flex items-center justify-end gap-3 md:col-span-2">
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => void navigate(-1)}
                    disabled={isPreConfirmando}
                    className="h-10 rounded-full border-zinc-700/60 bg-transparent px-5 text-sm text-zinc-400 hover:bg-zinc-800/50 hover:text-zinc-200"
                  >
                    Cancelar
                  </Button>
                  <Button
                    type="submit"
                    disabled={isPreConfirmando}
                    className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 disabled:opacity-60"
                  >
                    {isPreConfirmando ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden="true" />
                        Revisando…
                      </>
                    ) : (
                      'Revisar agendamento'
                    )}
                  </Button>
                </div>
              </form>
            </CardContent>
          </Card>

          <ResumoAgendamento servicosSelecionados={servicosSelecionados} />
        </div>
      )}
    </div>
  );
}
