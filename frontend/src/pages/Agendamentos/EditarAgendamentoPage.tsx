import { AxiosError } from 'axios';
import { AlertCircle, ArrowLeft, Car, Loader2, Lock, User } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { agendamentoService } from '@/services/agendamentoService';

import type { AgendaItemDetalhado } from '@/types/agenda';
import type { AgendamentoDetalhe, ResponsavelResumido } from '@/types/agendamento';

/** Converte ISO-8601 UTC para o valor local de um `<input type="datetime-local">`. */
function isoParaInputLocal(iso: string): string {
  const d = new Date(iso);
  const semFuso = new Date(d.getTime() - d.getTimezoneOffset() * 60000);
  return semFuso.toISOString().slice(0, 16);
}

/** Converte o valor de um `<input type="datetime-local">` para ISO-8601 UTC. */
function inputLocalParaIso(local: string): string {
  return new Date(local).toISOString();
}

/**
 * Edição de agendamento (RF010). O backend só permite alterar horário (início/fim),
 * responsável e observações, e apenas quando o status é AGENDADO. Cliente, veículo
 * e serviços são exibidos apenas para referência (não editáveis).
 */
export function EditarAgendamentoPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const location = useLocation();

  // Dados de exibição (cliente/veículo/serviços) vindos do modal de detalhe.
  const itemNav = (location.state as { item?: AgendaItemDetalhado } | null)?.item ?? null;

  const [detalhe, setDetalhe] = useState<AgendamentoDetalhe | null>(null);
  const [responsaveis, setResponsaveis] = useState<ResponsavelResumido[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);
  const [salvando, setSalvando] = useState(false);
  const [erroSalvar, setErroSalvar] = useState<string | null>(null);

  // Campos editáveis.
  const [inicio, setInicio] = useState('');
  const [fim, setFim] = useState('');
  const [responsavelId, setResponsavelId] = useState('');
  const [observacoes, setObservacoes] = useState('');

  useEffect(() => {
    if (!id) return;
    let ignore = false;

    const carregar = async () => {
      setCarregando(true);
      setErro(null);
      try {
        const dados = await agendamentoService.obterPorId(id);
        if (ignore) return;
        setDetalhe(dados);
        setInicio(isoParaInputLocal(dados.inicio));
        setFim(isoParaInputLocal(dados.fim));
        setResponsavelId(dados.responsavelId ?? '');
        setObservacoes(dados.observacoes ?? '');

        try {
          const resps = await agendamentoService.buscarResponsaveisPorCliente(dados.clienteId);
          if (!ignore) setResponsaveis(resps);
        } catch {
          // Lista de responsáveis é best-effort; mantém ao menos o atual.
        }
      } catch {
        if (!ignore) setErro('Não foi possível carregar o agendamento.');
      } finally {
        if (!ignore) setCarregando(false);
      }
    };

    void carregar();
    return () => {
      ignore = true;
    };
  }, [id]);

  // O GET /{id} serializa o status em minúsculo ("agendado"); normalizamos.
  const podeEditar = detalhe?.status?.toUpperCase() === 'AGENDADO';

  const handleSalvar = useCallback(async () => {
    if (!id || salvando) return;
    setErroSalvar(null);

    if (!inicio || !fim) {
      setErroSalvar('Informe início e fim.');
      return;
    }
    if (new Date(inicio).getTime() >= new Date(fim).getTime()) {
      setErroSalvar('O início deve ser anterior ao fim.');
      return;
    }
    if (!responsavelId) {
      setErroSalvar('Selecione o responsável.');
      return;
    }

    setSalvando(true);
    try {
      await agendamentoService.editar(id, {
        inicio: inicioParaEnvio(inicio, detalhe),
        fim: inputLocalParaIso(fim),
        responsavelId,
        observacoes: observacoes.trim() === '' ? null : observacoes.trim(),
      });
      void navigate('/agendamentos/calendario');
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        const data = error.response?.data as { title?: string; message?: string } | undefined;
        setErroSalvar(data?.message ?? data?.title ?? 'Erro ao salvar as alterações.');
      } else {
        setErroSalvar('Erro ao salvar as alterações.');
      }
    } finally {
      setSalvando(false);
    }
  }, [id, salvando, inicio, fim, responsavelId, observacoes, detalhe, navigate]);

  if (carregando) {
    return (
      <div className="flex h-full items-center justify-center text-zinc-400">
        <Loader2 className="h-6 w-6 animate-spin text-red-600" />
      </div>
    );
  }

  if (erro || !detalhe) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-3 text-center">
        <AlertCircle className="h-6 w-6 text-red-500" />
        <p className="text-sm text-zinc-400">{erro ?? 'Agendamento não encontrado.'}</p>
        <Button variant="outline" onClick={() => navigate('/agendamentos/calendario')}>
          Voltar ao calendário
        </Button>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-2xl px-6 py-6">
      <button
        type="button"
        onClick={() => navigate(-1)}
        className="mb-6 flex items-center gap-2 text-sm font-medium text-zinc-400 transition-colors hover:text-zinc-200"
      >
        <ArrowLeft className="h-4 w-4" />
        Voltar
      </button>

      <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-white">
        Editar agendamento
      </h1>
      <p className="mt-1 text-sm text-zinc-500">
        Cliente, veículo e serviços não são editáveis — apenas horário, responsável e observações
        (RF010).
      </p>

      {!podeEditar && (
        <div className="mt-4 flex items-start gap-2 rounded-xl border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-amber-500">
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
          <span>
            Este agendamento está com status <strong>{detalhe.status.toUpperCase()}</strong> e não pode ser
            editado. A edição só é permitida quando o status é AGENDADO.
          </span>
        </div>
      )}

      {/* Referência: cliente / veículo / serviços (read-only) */}
      {itemNav && (
        <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="rounded-xl border border-zinc-200 dark:border-zinc-800/60 bg-zinc-50 dark:bg-zinc-900/40 p-4">
            <h2 className="mb-2 flex items-center gap-1.5 text-xs font-semibold text-zinc-600 dark:text-zinc-300">
              <User className="h-3.5 w-3.5 text-red-500" /> Cliente <Lock className="h-3 w-3" />
            </h2>
            <p className="text-sm text-zinc-700 dark:text-zinc-200">{itemNav.cliente.nome}</p>
          </div>
          <div className="rounded-xl border border-zinc-200 dark:border-zinc-800/60 bg-zinc-50 dark:bg-zinc-900/40 p-4">
            <h2 className="mb-2 flex items-center gap-1.5 text-xs font-semibold text-zinc-600 dark:text-zinc-300">
              <Car className="h-3.5 w-3.5 text-red-500" /> Veículo <Lock className="h-3 w-3" />
            </h2>
            <p className="text-sm text-zinc-700 dark:text-zinc-200">
              {itemNav.veiculo.placa} — {itemNav.veiculo.modelo}
            </p>
          </div>
        </div>
      )}

      {/* Campos editáveis */}
      <div className="mt-6 space-y-5">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="space-y-1.5">
            <Label htmlFor="edit-inicio" className="text-xs font-bold tracking-wider text-zinc-500">
              INÍCIO
            </Label>
            <Input
              id="edit-inicio"
              type="datetime-local"
              value={inicio}
              disabled={!podeEditar || salvando}
              onChange={(e) => setInicio(e.target.value)}
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="edit-fim" className="text-xs font-bold tracking-wider text-zinc-500">
              FIM
            </Label>
            <Input
              id="edit-fim"
              type="datetime-local"
              value={fim}
              disabled={!podeEditar || salvando}
              onChange={(e) => setFim(e.target.value)}
            />
          </div>
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="edit-responsavel" className="text-xs font-bold tracking-wider text-zinc-500">
            RESPONSÁVEL
          </Label>
          <select
            id="edit-responsavel"
            value={responsavelId}
            disabled={!podeEditar || salvando}
            onChange={(e) => setResponsavelId(e.target.value)}
            className="h-10 w-full rounded-xl border border-zinc-200 dark:border-zinc-700/60 bg-zinc-50 dark:bg-zinc-950/40 px-3 text-sm text-zinc-800 dark:text-zinc-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500/50 disabled:opacity-60 dark:[color-scheme:dark]"
          >
            <option value="" disabled>
              Selecione o responsável
            </option>
            {responsavelId && !responsaveis.some((r) => r.id === responsavelId) && (
              <option value={responsavelId}>Responsável atual</option>
            )}
            {responsaveis.map((r) => (
              <option key={r.id} value={r.id}>
                {r.nome}
              </option>
            ))}
          </select>
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="edit-obs" className="text-xs font-bold tracking-wider text-zinc-500">
            OBSERVAÇÕES
          </Label>
          <textarea
            id="edit-obs"
            value={observacoes}
            disabled={!podeEditar || salvando}
            onChange={(e) => setObservacoes(e.target.value)}
            rows={4}
            maxLength={1000}
            placeholder="Observações do agendamento (opcional)"
            className="w-full rounded-xl border border-zinc-200 dark:border-zinc-700/60 bg-zinc-50 dark:bg-zinc-950/40 p-3 text-sm text-zinc-800 dark:text-zinc-200 placeholder:text-zinc-400 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500/50 disabled:opacity-60"
          />
        </div>

        {erroSalvar && (
          <div className="flex items-start gap-2 rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-500">
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
            <span>{erroSalvar}</span>
          </div>
        )}

        <div className="flex items-center justify-end gap-3 pt-2">
          <Button
            type="button"
            variant="outline"
            disabled={salvando}
            onClick={() => navigate(-1)}
          >
            Cancelar
          </Button>
          <Button
            type="button"
            disabled={!podeEditar || salvando}
            onClick={() => void handleSalvar()}
            className="bg-red-600 text-white hover:bg-red-700"
          >
            {salvando ? (
              <>
                <Loader2 className="mr-1.5 h-4 w-4 animate-spin" />
                Salvando…
              </>
            ) : (
              'Salvar alterações'
            )}
          </Button>
        </div>
      </div>
    </div>
  );
}

/**
 * Mantém o instante de início inalterado quando o usuário não tocou no campo
 * (evita reenviar um valor reconvertido com possível drift de fuso).
 */
function inicioParaEnvio(inputLocal: string, detalhe: AgendamentoDetalhe | null): string {
  if (detalhe && isoParaInputLocal(detalhe.inicio) === inputLocal) {
    return detalhe.inicio;
  }
  return inputLocalParaIso(inputLocal);
}
