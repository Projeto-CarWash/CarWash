import {
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Loader2,
  Pencil,
  Plus,
  Power,
  Search,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { clienteService, type ClienteResumo } from '@/services/clienteService';

const TAMANHO_PAGINA = 20;

export function ClientesListaPage() {
  const navigate = useNavigate();
  const [busca, setBusca] = useState('');
  const [pagina, setPagina] = useState(1);
  const [itens, setItens] = useState<ClienteResumo[]>([]);
  const [total, setTotal] = useState(0);
  const [carregando, setCarregando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [sucesso, setSucesso] = useState<string | null>(null);

  // ─── Estado da inativação/reativação ───
  const [clienteStatus, setClienteStatus] = useState<ClienteResumo | null>(null);
  const [modalStatusAberto, setModalStatusAberto] = useState(false);
  const [salvandoStatus, setSalvandoStatus] = useState(false);
  const [erroStatus, setErroStatus] = useState<string | null>(null);

  useEffect(() => {
    let cancelado = false;
    void (async () => {
      try {
        const resp = await clienteService.listar({
          busca: busca || undefined,
          pagina,
          tamanhoPagina: TAMANHO_PAGINA,
        });
        if (cancelado) return;
        setItens(resp.itens);
        setTotal(resp.total);
        setErro(null);
        setCarregando(false);
      } catch {
        if (cancelado) return;
        setErro('Não foi possível carregar a lista de clientes.');
        setCarregando(false);
      }
    })();
    return () => {
      cancelado = true;
    };
  }, [busca, pagina]);

  // ─── Mensagem de sucesso transitória ───
  useEffect(() => {
    if (!sucesso) return;
    const timer = setTimeout(() => setSucesso(null), 4000);
    return () => clearTimeout(timer);
  }, [sucesso]);

  const abrirModalStatus = (cliente: ClienteResumo) => {
    setErroStatus(null);
    setClienteStatus(cliente);
    setModalStatusAberto(true);
  };

  const confirmarAlteracaoStatus = async () => {
    if (!clienteStatus) return;
    const novoAtivo = !clienteStatus.ativo;
    setSalvandoStatus(true);
    setErroStatus(null);
    try {
      await clienteService.alterarStatus(clienteStatus.id, novoAtivo);
      // Atualiza o item local para refletir imediatamente na listagem
      setItens((prev) =>
        prev.map((c) => (c.id === clienteStatus.id ? { ...c, ativo: novoAtivo } : c)),
      );
      setSucesso(
        `Cliente "${clienteStatus.nome}" ${novoAtivo ? 'reativado' : 'inativado'} com sucesso.`,
      );
      setModalStatusAberto(false);
      setClienteStatus(null);
    } catch {
      setErroStatus(
        `Não foi possível ${novoAtivo ? 'reativar' : 'inativar'} o cliente "${clienteStatus.nome}".`,
      );
    } finally {
      setSalvandoStatus(false);
    }
  };

  const totalPaginas = Math.max(1, Math.ceil(total / TAMANHO_PAGINA));
  const podeVoltar = pagina > 1;
  const podeAvancar = pagina < totalPaginas;

  return (
    <div className="px-8 py-8">
      <div className="mb-6 flex items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-zinc-50">Clientes</h1>
          <p className="mt-1 text-sm text-zinc-500">
            {total === 0 ? 'Nenhum cliente cadastrado' : `${total} cliente(s) no total`}
          </p>
        </div>
        <Button
          type="button"
          onClick={() => void navigate('/clientes/novo')}
          className="h-10 rounded-full bg-red-600 px-5 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700"
        >
          <Plus className="mr-1 h-4 w-4" /> Novo cliente
        </Button>
      </div>

      <div className="mb-4 flex items-center gap-3">
        <div className="relative max-w-md flex-1">
          <Search
            className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-zinc-500"
            aria-hidden="true"
          />
          <Input
            type="text"
            value={busca}
            onChange={(e) => {
              setPagina(1);
              setBusca(e.target.value);
            }}
            placeholder="Buscar por nome, CPF, CNPJ, e-mail ou cidade…"
            className="h-10 rounded-xl border-zinc-700/60 bg-zinc-900/50 pl-9 text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0"
          />
        </div>
      </div>

      {erro && (
        <div
          role="alert"
          className="mb-4 rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3 text-sm text-red-400"
        >
          {erro}
        </div>
      )}

      {sucesso && (
        <div
          role="status"
          aria-live="polite"
          className="mb-4 flex items-center gap-2 rounded-xl border border-green-500/30 bg-green-950/30 px-4 py-3 text-sm text-green-400"
        >
          <CheckCircle2 className="h-4 w-4 shrink-0" aria-hidden="true" />
          {sucesso}
        </div>
      )}

      <div className="overflow-hidden rounded-2xl border border-zinc-800/60 bg-zinc-900/30">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-zinc-800/60 bg-zinc-900/60 text-[10px] font-bold uppercase tracking-[0.2em] text-zinc-500">
            <tr>
              <th className="px-4 py-3">Nome</th>
              <th className="px-4 py-3">Documento</th>
              <th className="px-4 py-3">Celular</th>
              <th className="px-4 py-3">Cidade/UF</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3 text-right">Ações</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-zinc-800/60">
            {carregando && (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-zinc-500">
                  Carregando…
                </td>
              </tr>
            )}
            {!carregando && itens.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-zinc-500">
                  Nenhum cliente encontrado.
                </td>
              </tr>
            )}
            {!carregando &&
              itens.map((c) => (
                <tr key={c.id} className="text-zinc-200 hover:bg-zinc-800/30">
                  <td className="px-4 py-3">
                    <Link
                      to={`/clientes/${c.id}`}
                      className="font-medium text-zinc-100 hover:text-red-400"
                    >
                      {c.nome}
                    </Link>
                  </td>
                  <td className="px-4 py-3 tabular-nums text-zinc-400">
                    {formatarDocumento(c.cpf, c.cnpj)}
                  </td>
                  <td className="px-4 py-3 tabular-nums text-zinc-400">
                    {formatarCelular(c.celular)}
                  </td>
                  <td className="px-4 py-3 text-zinc-400">
                    {c.cidade} / {c.uf}
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={
                        c.ativo
                          ? 'rounded-full bg-green-500/10 px-2 py-1 text-[10px] font-bold tracking-[0.15em] text-green-400'
                          : 'rounded-full bg-zinc-700/40 px-2 py-1 text-[10px] font-bold tracking-[0.15em] text-zinc-500'
                      }
                    >
                      {c.ativo ? 'ATIVO' : 'INATIVO'}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex justify-end gap-2">
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() => void navigate(`/clientes/${c.id}/editar`)}
                        title="Editar cliente"
                        className="h-8 w-8 text-zinc-400 hover:bg-zinc-800 hover:text-zinc-100"
                      >
                        <Pencil className="h-4 w-4" />
                        <span className="sr-only">Editar {c.nome}</span>
                      </Button>
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() => abrirModalStatus(c)}
                        title={c.ativo ? 'Inativar cliente' : 'Reativar cliente'}
                        className="h-8 w-8 text-zinc-400 hover:bg-zinc-800 hover:text-zinc-100"
                      >
                        <Power className="h-4 w-4" />
                        <span className="sr-only">
                          {c.ativo ? 'Inativar' : 'Reativar'} {c.nome}
                        </span>
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      <div className="mt-4 flex items-center justify-between">
        <span className="text-xs text-zinc-500">
          Página {pagina} de {totalPaginas}
        </span>
        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant="outline"
            disabled={!podeVoltar}
            onClick={() => setPagina((p) => Math.max(1, p - 1))}
            className="h-9 rounded-full border-zinc-700/60 bg-transparent px-3 text-sm disabled:opacity-40"
          >
            <ChevronLeft className="h-4 w-4" /> Anterior
          </Button>
          <Button
            type="button"
            variant="outline"
            disabled={!podeAvancar}
            onClick={() => setPagina((p) => p + 1)}
            className="h-9 rounded-full border-zinc-700/60 bg-transparent px-3 text-sm disabled:opacity-40"
          >
            Próxima <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* ─── Modal de confirmação de inativação/reativação ─── */}
      <Dialog
        open={modalStatusAberto}
        onOpenChange={(aberto) => {
          if (salvandoStatus) return;
          setModalStatusAberto(aberto);
          if (!aberto) {
            setClienteStatus(null);
            setErroStatus(null);
          }
        }}
      >
        <DialogContent className="sm:max-w-[425px] bg-zinc-950 border-zinc-800">
          <DialogHeader>
            <DialogTitle className="text-zinc-100">
              {clienteStatus?.ativo ? 'Inativar cliente' : 'Reativar cliente'}
            </DialogTitle>
            <DialogDescription className="text-zinc-400">
              {clienteStatus?.ativo
                ? `Deseja realmente inativar o cliente "${clienteStatus?.nome}"? Ele não estará mais disponível para novos agendamentos.`
                : `Deseja reativar o cliente "${clienteStatus?.nome}"? Ele voltará a estar disponível para novos agendamentos.`}
            </DialogDescription>
          </DialogHeader>

          {erroStatus && (
            <p role="alert" className="text-sm text-red-400">
              {erroStatus}
            </p>
          )}

          <DialogFooter className="mt-4 gap-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => setModalStatusAberto(false)}
              disabled={salvandoStatus}
              className="border-zinc-700 bg-transparent text-zinc-300 hover:bg-zinc-800 hover:text-zinc-100"
            >
              Cancelar
            </Button>
            <Button
              type="button"
              onClick={() => void confirmarAlteracaoStatus()}
              disabled={salvandoStatus}
              className="bg-red-600 text-white hover:bg-red-700"
            >
              {salvandoStatus ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
              Confirmar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function formatarDocumento(cpf?: string, cnpj?: string): string {
  if (cpf?.length === 11) {
    return `${cpf.slice(0, 3)}.${cpf.slice(3, 6)}.${cpf.slice(6, 9)}-${cpf.slice(9)}`;
  }
  if (cnpj?.length === 14) {
    return `${cnpj.slice(0, 2)}.${cnpj.slice(2, 5)}.${cnpj.slice(5, 8)}/${cnpj.slice(8, 12)}-${cnpj.slice(12)}`;
  }
  return '—';
}

function formatarCelular(celular: string): string {
  if (celular.length !== 11) return celular;
  return `(${celular.slice(0, 2)}) ${celular.slice(2, 7)}-${celular.slice(7)}`;
}
