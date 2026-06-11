import {
  ChevronLeft,
  ChevronRight,
  Eye,
  Filter,
  Loader2,
  Pencil,
  Plus,
  Power,
  Search,
} from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

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
  const [erroStatus, setErroStatus] = useState<string | null>(null);
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
    setErroStatus(null);
    setModalStatusAberto(true);
  }, []);

  const confirmarAlteracaoStatus = useCallback(async () => {
    if (!usuarioAtivacao) return;
    const novoAtivo = !usuarioAtivacao.ativo;
    setSalvandoStatus(true);
    setErro(null);
    setErroStatus(null);
    try {
      await userService.updateStatus(usuarioAtivacao.id, novoAtivo);
      // Atualiza o item local para feedback imediato
      setItens((prev) =>
        prev.map((u) => (u.id === usuarioAtivacao.id ? { ...u, ativo: novoAtivo } : u)),
      );
      setModalStatusAberto(false);
      setUsuarioAtivacao(null);
    } catch {
      // O modal permanece aberto: a mensagem precisa aparecer DENTRO do dialog,
      // pois o restante da página fica aria-hidden enquanto o modal está aberto.
      setErroStatus(
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
          <h1 className="text-3xl font-bold tracking-tight text-foreground">Usuários internos</h1>
          <p className="mt-1 text-sm text-muted-foreground">
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
            className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
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
            className="h-10 rounded-xl border-border bg-card pl-9 text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0"
          />
        </div>

        {/* ── Filtro por status ── */}
        <div className="flex items-center gap-1.5 rounded-xl border border-border bg-card p-1">
          <Filter className="ml-2 h-3.5 w-3.5 text-muted-foreground" aria-hidden="true" />
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
                  ? 'bg-muted text-foreground'
                  : 'text-muted-foreground hover:text-foreground'
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

      <div className="overflow-hidden rounded-2xl border border-border bg-card">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-border bg-card text-[10px] font-bold uppercase tracking-[0.2em] text-muted-foreground">
            <tr>
              <th className="px-4 py-3">Nome</th>
              <th className="px-4 py-3">E-mail</th>
              <th className="px-4 py-3">Perfil</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3 text-center">Ações</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {carregando && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">
                  Carregando…
                </td>
              </tr>
            )}
            {!carregando && itens.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">
                  Nenhum usuário encontrado.
                </td>
              </tr>
            )}
            {!carregando &&
              itens.map((u) => (
                <tr key={u.id} className="text-foreground hover:bg-accent">
                  <td className="px-4 py-3">
                    <span className="font-medium text-foreground">{u.nome}</span>
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">{u.email}</td>
                  <td className="px-4 py-3 text-muted-foreground">{u.perfil}</td>
                  <td className="px-4 py-3">
                    <span
                      className={
                        u.ativo
                          ? 'rounded-full bg-green-500/10 px-2 py-1 text-[10px] font-bold tracking-[0.15em] text-green-400'
                          : 'rounded-full bg-muted px-2 py-1 text-[10px] font-bold tracking-[0.15em] text-muted-foreground'
                      }
                    >
                      {u.ativo ? 'ATIVO' : 'INATIVO'}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-center">
                    <div className="flex justify-center gap-2">
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() => void navigate(`/usuarios/${u.id}`)}
                        title="Visualizar usuário"
                        className="h-8 w-8 text-muted-foreground hover:bg-accent hover:text-foreground"
                      >
                        <Eye className="h-4 w-4" />
                        <span className="sr-only">Visualizar {u.nome}</span>
                      </Button>
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() => void navigate(`/usuarios/${u.id}`)}
                        title="Editar usuário"
                        className="h-8 w-8 text-muted-foreground hover:bg-accent hover:text-foreground"
                      >
                        <Pencil className="h-4 w-4" />
                        <span className="sr-only">Editar {u.nome}</span>
                      </Button>
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() => abrirModalStatus(u)}
                        title={u.ativo ? 'Inativar usuário' : 'Ativar usuário'}
                        className="h-8 w-8 text-muted-foreground hover:bg-accent hover:text-foreground"
                        aria-label={`${u.ativo ? 'Inativar' : 'Ativar'} ${u.nome}`}
                      >
                        <Power className="h-4 w-4" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      <div className="mt-4 flex items-center justify-between">
        <span className="text-xs text-muted-foreground">
          Página {pagina} de {totalPaginas}
        </span>
        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant="outline"
            disabled={pagina <= 1}
            onClick={() => setPagina((p) => Math.max(1, p - 1))}
            className="h-9 rounded-full border-border bg-transparent px-3 text-sm disabled:opacity-40"
          >
            <ChevronLeft className="h-4 w-4" /> Anterior
          </Button>
          <Button
            type="button"
            variant="outline"
            disabled={pagina >= totalPaginas}
            onClick={() => setPagina((p) => p + 1)}
            className="h-9 rounded-full border-border bg-transparent px-3 text-sm disabled:opacity-40"
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
          if (!aberto) {
            setUsuarioAtivacao(null);
            setErroStatus(null);
          }
        }}
      >
        <DialogContent className="sm:max-w-[425px] bg-background border-border">
          <DialogHeader>
            <DialogTitle className="text-foreground">
              {usuarioAtivacao?.ativo ? 'Inativar usuário' : 'Ativar usuário'}
            </DialogTitle>
            <DialogDescription className="text-muted-foreground">
              {usuarioAtivacao?.ativo
                ? `Deseja realmente inativar o usuário "${usuarioAtivacao?.nome}"? O acesso dele ao sistema será bloqueado.`
                : `Deseja reativar o usuário "${usuarioAtivacao?.nome}"? O acesso dele ao sistema será restaurado.`}
            </DialogDescription>
          </DialogHeader>
          {erroStatus && (
            <div
              role="alert"
              className="rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3 text-sm text-red-400"
            >
              {erroStatus}
            </div>
          )}
          <DialogFooter className="mt-4 gap-2 sm:gap-0">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setModalStatusAberto(false);
                setUsuarioAtivacao(null);
                setErroStatus(null);
              }}
              disabled={salvandoStatus}
              className="border-border bg-transparent text-foreground hover:bg-accent hover:text-foreground"
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
