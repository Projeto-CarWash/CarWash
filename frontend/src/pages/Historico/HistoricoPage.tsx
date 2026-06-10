import {
  AlertCircle,
  CalendarSearch,
  History,
  Loader2,
  RotateCw,
  SearchX,
  XCircle,
} from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useClientesParaAgendamento } from '@/hooks/useAgendamentoQueries';
import { useHistoricoCliente } from '@/hooks/useHistoricoQuery';
import { tratarErroApi } from '@/lib/apiError';
import { historicoService } from '@/services/historicoService';

import { HistoricoDetalheModal } from './HistoricoDetalheModal';
import {
  mensagemErroHistorico,
  MSG_DATA_INVALIDA,
  MSG_FILTRO_INVALIDO,
  MSG_SUCESSO,
  MSG_VAZIO,
} from './historicoFormat';
import { HistoricoLista } from './HistoricoLista';

import type { AgendaStatus } from '@/types/agenda';
import type { HistoricoFiltros, HistoricoItem } from '@/types/historico';

/** Opções de status para o seletor de filtro. */
const STATUS_OPCOES: { valor: AgendaStatus; rotulo: string }[] = [
  { valor: 'AGENDADO', rotulo: 'Agendado' },
  { valor: 'EM_ANDAMENTO', rotulo: 'Em andamento' },
  { valor: 'CONCLUIDO', rotulo: 'Concluído' },
  { valor: 'CANCELADO', rotulo: 'Cancelado' },
];

/** Opções do filtro "últimos dias". */
const ULTIMOS_DIAS_OPCOES: { valor: number; rotulo: string }[] = [
  { valor: 7, rotulo: 'Últimos 7 dias' },
  { valor: 15, rotulo: 'Últimos 15 dias' },
  { valor: 30, rotulo: 'Últimos 30 dias' },
  { valor: 60, rotulo: 'Últimos 60 dias' },
  { valor: 90, rotulo: 'Últimos 90 dias' },
];

const classeCampo =
  'h-10 rounded-lg border border-zinc-700/60 bg-zinc-950/40 px-3 text-sm text-zinc-100 focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none disabled:opacity-50';

/**
 * Tela de histórico de atendimentos por cliente (RF012).
 *
 * <p>Permite buscar e visualizar o histórico de atendimentos de um cliente
 * selecionado, com filtros de período (intervalo de datas ou últimos dias),
 * status, e acesso ao detalhe do atendimento.</p>
 *
 * <p>Consome `GET /api/v1/agenda?formato=detalhado&clienteId={id}` via
 * `historicoService` — sem criação de endpoints.</p>
 */
export function HistoricoPage() {
  const navigate = useNavigate();

  // Filtros em edição
  const [clienteId, setClienteId] = useState('');
  const [dataInicio, setDataInicio] = useState('');
  const [dataFim, setDataFim] = useState('');
  const [ultimosDias, setUltimosDias] = useState<number | ''>('');
  const [status, setStatus] = useState<AgendaStatus | ''>('');
  const [erroLocal, setErroLocal] = useState<string | null>(null);

  // Filtros aplicados (enviados à query)
  const [filtrosAplicados, setFiltrosAplicados] = useState<HistoricoFiltros | null>(null);

  // Filial padrão (obtida automaticamente)
  const [filialId, setFilialId] = useState('');

  // Item selecionado para detalhe
  const [itemDetalhe, setItemDetalhe] = useState<HistoricoItem | null>(null);

  // Toast de feedback
  const [toast, setToast] = useState<{ tipo: 'sucesso' | 'erro'; mensagem: string } | null>(null);

  // Clientes para seletor
  const clientesQuery = useClientesParaAgendamento('');
  const clientes = useMemo(() => clientesQuery.data?.itens ?? [], [clientesQuery.data]);

  // Query do histórico
  const historicoQuery = useHistoricoCliente(filtrosAplicados);

  // Obtém filial padrão no mount
  useEffect(() => {
    void historicoService.obterFilialPadrao().then((id) => {
      if (id) setFilialId(id);
    });
  }, []);

  // Auto-dismiss toast
  useEffect(() => {
    let timer: ReturnType<typeof setTimeout> | undefined;
    if (toast) {
      timer = setTimeout(() => setToast(null), 5000);
    }
    return () => {
      if (timer) clearTimeout(timer);
    };
  }, [toast]);

  // Mensagem de sucesso derivada do estado da query (sem efeito colateral)
  const mensagemSucesso =
    historicoQuery.isSuccess && historicoQuery.data && historicoQuery.data.itens.length > 0
      ? MSG_SUCESSO
      : null;

  /** Limpa resultados ao trocar de cliente. */
  function handleClienteChange(novoClienteId: string) {
    setClienteId(novoClienteId);
    setFiltrosAplicados(null);
    setItemDetalhe(null);
    setErroLocal(null);
    setToast(null);
  }

  /** Valida e aplica filtros. */
  function handleBuscar(e: React.FormEvent) {
    e.preventDefault();
    setErroLocal(null);

    if (!clienteId) {
      setErroLocal('Selecione um cliente para consultar o histórico.');
      return;
    }

    // Validação: não permitir combinação
    const temIntervalo = dataInicio !== '' || dataFim !== '';
    const temUltimosDias = ultimosDias !== '';

    if (temIntervalo && temUltimosDias) {
      setErroLocal(MSG_FILTRO_INVALIDO);
      return;
    }

    // Validação: dataInicio <= dataFim
    if (dataInicio && dataFim && dataInicio > dataFim) {
      setErroLocal(MSG_DATA_INVALIDA);
      return;
    }

    const filtros: HistoricoFiltros = {
      clienteId,
      filialId,
      dataInicio: temIntervalo && dataInicio ? dataInicio : undefined,
      dataFim: temIntervalo && dataFim ? dataFim : undefined,
      ultimosDias: temUltimosDias ? Number(ultimosDias) : undefined,
      status: status || '',
    };

    setFiltrosAplicados(filtros);
  }

  /** Limpa todos os filtros e resultados. */
  function handleLimpar() {
    setDataInicio('');
    setDataFim('');
    setUltimosDias('');
    setStatus('');
    setErroLocal(null);
    setFiltrosAplicados(null);
    setItemDetalhe(null);
    setToast(null);
  }

  const consultou = filtrosAplicados !== null;
  const itens = historicoQuery.data?.itens ?? [];
  const erroApi = historicoQuery.isError ? tratarErroApi(historicoQuery.error) : null;

  // Tratamento de erro HTTP com mensagens específicas do RF012
  const mensagemErro = erroApi ? mensagemErroHistorico(erroApi.status) : null;

  // Redirect para login em 401
  useEffect(() => {
    if (erroApi?.status === 401) {
      void navigate('/login');
    }
  }, [erroApi, navigate]);

  const botaoBuscarDesabilitado = !clienteId || historicoQuery.isFetching;

  return (
    <div className="px-4 py-6 sm:px-8 sm:py-8">
      {/* Cabeçalho */}
      <div className="mb-6 flex items-center gap-3">
        <span
          className="flex h-10 w-10 items-center justify-center rounded-lg bg-red-600/10 text-red-500"
          aria-hidden="true"
        >
          <History className="h-5 w-5" />
        </span>
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-zinc-50">
            Histórico de Atendimentos
          </h1>
          <p className="mt-1 text-sm text-zinc-400">
            Consulte o histórico de atendimentos por cliente (RF012).
          </p>
        </div>
      </div>

      {/* Mensagem de sucesso (derivada do estado da query) */}
      {mensagemSucesso && (
        <div
          role="status"
          aria-live="polite"
          className="mb-4 flex items-center gap-3 rounded-xl border border-green-500/30 bg-green-950/30 px-4 py-3 text-sm font-medium text-green-400"
        >
          <CalendarSearch className="h-4 w-4 shrink-0" aria-hidden="true" />
          {mensagemSucesso}
        </div>
      )}

      {/* Toast de erro (imperativo) */}
      {toast && (
        <div
          role="alert"
          aria-live="assertive"
          className="mb-4 flex items-center gap-3 rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3 text-sm font-medium text-red-400"
        >
          <AlertCircle className="h-4 w-4 shrink-0" aria-hidden="true" />
          {toast.mensagem}
        </div>
      )}

      {/* Barra de filtros */}
      <form
        onSubmit={handleBuscar}
        aria-label="Filtros do histórico de atendimentos"
        className="mb-6 rounded-2xl border border-zinc-800/60 bg-zinc-900/30 p-4"
      >
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {/* Cliente (obrigatório) */}
          <div className="flex flex-col gap-1.5 sm:col-span-2 lg:col-span-3">
            <Label htmlFor="hist-cliente" className="text-zinc-300">
              Cliente <span className="text-red-400">*</span>
            </Label>
            <select
              id="hist-cliente"
              value={clienteId}
              onChange={(e) => handleClienteChange(e.target.value)}
              disabled={clientesQuery.isLoading}
              className={classeCampo}
              aria-required="true"
            >
              <option value="">
                {clientesQuery.isLoading ? 'Carregando clientes…' : 'Selecione um cliente'}
              </option>
              {clientes.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.nome} {c.cpf ? `(CPF: ...${c.cpf.slice(-4)})` : c.cnpj ? `(CNPJ: ...${c.cnpj.slice(-4)})` : ''}
                </option>
              ))}
            </select>
            {clientesQuery.isError && (
              <p className="text-xs text-amber-400" role="alert">
                Não foi possível carregar a lista de clientes.
              </p>
            )}
          </div>

          {/* Data início */}
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="hist-data-inicio" className="text-zinc-300">
              Data início
            </Label>
            <Input
              id="hist-data-inicio"
              type="date"
              value={dataInicio}
              onChange={(e) => setDataInicio(e.target.value)}
              className="h-10 rounded-lg dark:[color-scheme:dark]"
              disabled={ultimosDias !== ''}
              aria-describedby={ultimosDias !== '' ? 'hist-aviso-periodo' : undefined}
            />
          </div>

          {/* Data fim */}
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="hist-data-fim" className="text-zinc-300">
              Data fim
            </Label>
            <Input
              id="hist-data-fim"
              type="date"
              value={dataFim}
              onChange={(e) => setDataFim(e.target.value)}
              className="h-10 rounded-lg dark:[color-scheme:dark]"
              disabled={ultimosDias !== ''}
            />
          </div>

          {/* Últimos dias */}
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="hist-ultimos-dias" className="text-zinc-300">
              Últimos dias
            </Label>
            <select
              id="hist-ultimos-dias"
              value={ultimosDias}
              onChange={(e) => {
                const val = e.target.value;
                setUltimosDias(val === '' ? '' : Number(val));
                if (val !== '') {
                  setDataInicio('');
                  setDataFim('');
                }
              }}
              disabled={dataInicio !== '' || dataFim !== ''}
              className={classeCampo}
              aria-describedby={dataInicio || dataFim ? 'hist-aviso-periodo' : undefined}
            >
              <option value="">Selecione</option>
              {ULTIMOS_DIAS_OPCOES.map((o) => (
                <option key={o.valor} value={o.valor}>
                  {o.rotulo}
                </option>
              ))}
            </select>
            {((dataInicio || dataFim) && ultimosDias !== '') && (
              <p id="hist-aviso-periodo" className="text-xs text-amber-400" role="alert">
                Escolha intervalo de datas ou últimos dias, não ambos.
              </p>
            )}
          </div>

          {/* Status */}
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="hist-status" className="text-zinc-300">
              Status <span className="text-zinc-500">(opcional)</span>
            </Label>
            <select
              id="hist-status"
              value={status}
              onChange={(e) => setStatus(e.target.value as AgendaStatus | '')}
              className={classeCampo}
            >
              <option value="">Todos os status</option>
              {STATUS_OPCOES.map((s) => (
                <option key={s.valor} value={s.valor}>
                  {s.rotulo}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Ações */}
        <div className="mt-4 flex flex-wrap items-center justify-end gap-3">
          <Button
            type="button"
            variant="outline"
            onClick={handleLimpar}
            className="h-10 rounded-full border-zinc-700/60 px-4 text-sm text-zinc-300 hover:bg-zinc-800"
          >
            <XCircle className="mr-1 h-4 w-4" aria-hidden="true" />
            Limpar filtros
          </Button>
          <Button
            type="submit"
            disabled={botaoBuscarDesabilitado}
            className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 disabled:opacity-50"
          >
            <CalendarSearch className="mr-1 h-4 w-4" aria-hidden="true" />
            Buscar histórico
          </Button>
        </div>

        {/* Erro de validação local */}
        {erroLocal && (
          <div
            role="alert"
            aria-live="assertive"
            className="mt-4 flex items-start gap-3 rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3"
          >
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-red-500" aria-hidden="true" />
            <p className="text-sm font-medium text-red-400">{erroLocal}</p>
          </div>
        )}
      </form>

      {/* Resultados */}
      <section aria-label="Resultados do histórico" aria-busy={historicoQuery.isFetching}>
        {/* Estado inicial */}
        {!consultou && (
          <div className="flex flex-col items-center justify-center rounded-2xl border border-zinc-800/60 bg-zinc-900/20 px-4 py-16 text-center">
            <History className="mb-3 h-10 w-10 text-zinc-600" aria-hidden="true" />
            <p className="text-sm font-medium text-zinc-400">
              Selecione um cliente e clique em &ldquo;Buscar histórico&rdquo;
            </p>
            <p className="mt-1 text-xs text-zinc-500">
              Os filtros de período e status são opcionais.
            </p>
          </div>
        )}

        {/* Loading */}
        {consultou && historicoQuery.isLoading && (
          <div
            role="status"
            className="flex items-center justify-center gap-2 rounded-2xl border border-zinc-800/60 bg-zinc-900/20 px-4 py-16 text-sm text-zinc-400"
          >
            <Loader2 className="h-5 w-5 animate-spin text-red-500" aria-hidden="true" />
            Consultando histórico…
          </div>
        )}

        {/* Erro */}
        {consultou && erroApi && (
          <div
            role="alert"
            aria-live="assertive"
            className="rounded-2xl border border-red-500/30 bg-red-950/30 px-4 py-8 text-center"
          >
            <AlertCircle className="mx-auto mb-2 h-6 w-6 text-red-500" aria-hidden="true" />
            <p className="text-sm font-medium text-red-400">{mensagemErro}</p>

            {/* Bloquear conteúdo no 403 */}
            {erroApi.status === 403 && (
              <p className="mt-2 text-xs text-zinc-500">
                Entre em contato com o administrador do sistema.
              </p>
            )}

            {/* Botão retry para 500 e erros de rede */}
            {(erroApi.status === 500 || erroApi.status === null) && (
              <Button
                type="button"
                variant="outline"
                onClick={() => void historicoQuery.refetch()}
                className="mt-4 h-9 rounded-full border-zinc-700/60 px-4 text-sm"
              >
                <RotateCw className="mr-1 h-4 w-4" aria-hidden="true" />
                Tentar novamente
              </Button>
            )}
          </div>
        )}

        {/* Vazio */}
        {consultou && historicoQuery.isSuccess && itens.length === 0 && (
          <div className="flex flex-col items-center justify-center rounded-2xl border border-zinc-800/60 bg-zinc-900/20 px-4 py-16 text-center">
            <SearchX className="mb-3 h-10 w-10 text-zinc-600" aria-hidden="true" />
            <p className="text-sm font-medium text-zinc-400" role="status" aria-live="polite">
              {MSG_VAZIO}
            </p>
          </div>
        )}

        {/* Sucesso com dados */}
        {consultou && historicoQuery.isSuccess && itens.length > 0 && (
          <>
            <div className="mb-3 flex items-center justify-between">
              <p className="text-xs text-zinc-400">
                {itens.length} atendimento{itens.length !== 1 ? 's' : ''} encontrado
                {itens.length !== 1 ? 's' : ''}.
              </p>
              <Button
                type="button"
                variant="outline"
                onClick={() => void historicoQuery.refetch()}
                disabled={historicoQuery.isFetching}
                className="h-8 rounded-full border-zinc-700/60 px-3 text-xs"
              >
                <RotateCw
                  className={`mr-1 h-3.5 w-3.5 ${historicoQuery.isFetching ? 'animate-spin' : ''}`}
                  aria-hidden="true"
                />
                Atualizar
              </Button>
            </div>
            <HistoricoLista itens={itens} onVerDetalhe={setItemDetalhe} />
          </>
        )}
      </section>

      {/* Modal de detalhe */}
      {itemDetalhe && (
        <HistoricoDetalheModal item={itemDetalhe} onFechar={() => setItemDetalhe(null)} />
      )}
    </div>
  );
}
