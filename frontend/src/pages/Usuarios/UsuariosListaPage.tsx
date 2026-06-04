import { ChevronLeft, ChevronRight, Filter, Loader2, Plus, Power, Search } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
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
import { userService } from '@/services/userService';

import type { UsuarioResponse } from '@/types/user';

const TAMANHO_PAGINA = 20;

type FiltroStatus = 'todos' | 'ativo' | 'inativo';

export function UsuariosListaPage() {
  const navigate = useNavigate();
  const [busca, setBusca] = useState('');
  const [filtroStatus, setFiltroStatus] = useState<FiltroStatus>('todos');
  const [pagina, setPagina] = useState(1);
  const [itens, setItens] = useState<UsuarioResponse[]>([]);
  const [total, setTotal] = useState(0);
  const [carregando, setCarregando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [usuarioAtivacao, setUsuarioAtivacao] = useState<UsuarioResponse | null>(null);
  const [modalStatusAberto, setModalStatusAberto] = useState(false);
  const [salvandoStatus, setSalvandoStatus] = useState(false);

  const carregarUsuarios = useCallback(async () => {
    setCarregando(true);
    try {
      const ativo = filtroStatus === 'todos' ? undefined : filtroStatus === 'ativo';
      const resp = await userService.list({
        busca: busca || undefined,
        ativo,
        pagina,
        tamanhoPagina: TAMANHO_PAGINA,
      });
      setItens(resp.itens);
      setTotal(resp.total);
      setErro(null);
    } catch {
      setErro('Não foi possível carregar a lista de usuários.');
    } finally {
      setCarregando(false);
    }
  }, [busca, filtroStatus, pagina]);

  useEffect(() => {
    let cancelado = false;
    void (async () => {
      await carregarUsuarios();
      if (cancelado) return;
    })();
    return () => {
      cancelado = true;
    };
  }, [carregarUsuarios]);

  const abrirModalStatus = useCallback((usuario: UsuarioResponse) => {
    setUsuarioAtivacao(usuario);
    setModalStatusAberto(true);
  }, []);

  const confirmarAlteracaoStatus = useCallback(async () => {
    if (!usuarioAtivacao) return;
    const novoAtivo = !usuarioAtivacao.ativo;
    setSalvandoStatus(true);
    setErro(null);
    try {
      await userService.updateStatus(usuarioAtivacao.id, novoAtivo);
      // Atualiza o item local para feedback imediato
      setItens((prev) =>
        prev.map((u) => (u.id === usuarioAtivacao.id ? { ...u, ativo: novoAtivo } : u)),
      );
      setModalStatusAberto(false);
      setUsuarioAtivacao(null);
    } catch {
      setErro(
        `Não foi possível ${novoAtivo ? 'ativar' : 'inativar'} o usuário "${usuarioAtivacao.nome}".`,
      );
    } finally {
      setSalvandoStatus(false);
    }
  }, [usuarioAtivacao]);

  const totalPaginas = Math.max(1, Math.ceil(total / TAMANHO_PAGINA));

  return (
    <div className="px-8 py-8">
      <div className="mb-6 flex items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-zinc-50">Usuários internos</h1>
          <p className="mt-1 text-sm text-zinc-500">
            {total === 0 ? 'Nenhum usuário cadastrado' : `${total} usuário(s) no total`}
          </p>
        </div>
        <Button
          type="button"
          onClick={() => void navigate('/usuarios/novo')}
          className="h-10 rounded-full bg-red-600 px-5 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700"
        >
          <Plus className="mr-1 h-4 w-4" /> Novo usuário
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
            placeholder="Buscar por nome ou e-mail…"
            className="h-10 rounded-xl border-zinc-700/60 bg-zinc-900/50 pl-9 text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0"
          />
        </div>

        {/* ── Filtro por status ── */}
        <div className="flex items-center gap-1.5 rounded-xl border border-zinc-700/60 bg-zinc-900/50 p-1">
          <Filter className="ml-2 h-3.5 w-3.5 text-zinc-500" aria-hidden="true" />
          {(['todos', 'ativo', 'inativo'] as FiltroStatus[]).map((f) => (
            <button
              key={f}
              type="button"
              onClick={() => {
                setPagina(1);
                setFiltroStatus(f);
              }}
              className={`rounded-lg px-3 py-1.5 text-[11px] font-semibold tracking-wide transition-colors ${
                filtroStatus === f
                  ? 'bg-zinc-700/60 text-zinc-100'
                  : 'text-zinc-500 hover:text-zinc-300'
              }`}
            >
              {f === 'todos' ? 'Todos' : f === 'ativo' ? 'Ativos' : 'Inativos'}
            </button>
          ))}
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

      <div className="overflow-hidden rounded-2xl border border-zinc-800/60 bg-zinc-900/30">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-zinc-800/60 bg-zinc-900/60 text-[10px] font-bold uppercase tracking-[0.2em] text-zinc-500">
            <tr>
              <th className="px-4 py-3">Nome</th>
              <th className="px-4 py-3">E-mail</th>
              <th className="px-4 py-3">Perfil</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3 text-center">Ações</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-zinc-800/60">
            {carregando && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-zinc-500">
                  Carregando…
                </td>
              </tr>
            )}
            {!carregando && itens.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-zinc-500">
                  Nenhum usuário encontrado.
                </td>
              </tr>
            )}
            {!carregando &&
              itens.map((u) => (
                <tr key={u.id} className="text-zinc-200 hover:bg-zinc-800/30">
                  <td className="px-4 py-3">
                    <Link
                      to={`/usuarios/${u.id}`}
                      className="font-medium text-zinc-100 hover:text-red-400"
                    >
                      {u.nome}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-zinc-400">{u.email}</td>
                  <td className="px-4 py-3 text-zinc-400">{u.perfil}</td>
                  <td className="px-4 py-3">
                    <span
                      className={
                        u.ativo
                          ? 'rounded-full bg-green-500/10 px-2 py-1 text-[10px] font-bold tracking-[0.15em] text-green-400'
                          : 'rounded-full bg-zinc-700/40 px-2 py-1 text-[10px] font-bold tracking-[0.15em] text-zinc-500'
                      }
                    >
                      {u.ativo ? 'ATIVO' : 'INATIVO'}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-center">
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => abrirModalStatus(u)}
                      className="h-8 w-8 text-zinc-400 hover:bg-zinc-800 hover:text-zinc-100"
                      aria-label={`${u.ativo ? 'Inativar' : 'Ativar'} ${u.nome}`}
                    >
                      <Power className="h-4 w-4" />
                    </Button>
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
            disabled={pagina <= 1}
            onClick={() => setPagina((p) => Math.max(1, p - 1))}
            className="h-9 rounded-full border-zinc-700/60 bg-transparent px-3 text-sm disabled:opacity-40"
          >
            <ChevronLeft className="h-4 w-4" /> Anterior
          </Button>
          <Button
            type="button"
            variant="outline"
            disabled={pagina >= totalPaginas}
            onClick={() => setPagina((p) => p + 1)}
            className="h-9 rounded-full border-zinc-700/60 bg-transparent px-3 text-sm disabled:opacity-40"
          >
            Próxima <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* ── Modal de confirmação de ativação/inativação ── */}
      <Dialog
        open={modalStatusAberto}
        onOpenChange={(aberto) => {
          if (salvandoStatus) return;
          setModalStatusAberto(aberto);
          if (!aberto) setUsuarioAtivacao(null);
        }}
      >
        <DialogContent className="sm:max-w-[425px] bg-zinc-950 border-zinc-800">
          <DialogHeader>
            <DialogTitle className="text-zinc-100">
              {usuarioAtivacao?.ativo ? 'Inativar usuário' : 'Ativar usuário'}
            </DialogTitle>
            <DialogDescription className="text-zinc-400">
              {usuarioAtivacao?.ativo
                ? `Deseja realmente inativar o usuário "${usuarioAtivacao?.nome}"? O acesso dele ao sistema será bloqueado.`
                : `Deseja reativar o usuário "${usuarioAtivacao?.nome}"? O acesso dele ao sistema será restaurado.`}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="mt-4 gap-2 sm:gap-0">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setModalStatusAberto(false);
                setUsuarioAtivacao(null);
              }}
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
