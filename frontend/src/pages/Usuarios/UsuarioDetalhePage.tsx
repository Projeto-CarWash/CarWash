import { AlertCircle, ArrowLeft, Loader2, Power, Save, X } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { userService } from '@/services/userService';

import type { PerfilUsuario } from '@/types/auth';
import type { UsuarioResponse } from '@/types/user';

const PERFIS: PerfilUsuario[] = ['Admin', 'Funcionario'];

export function UsuarioDetalhePage() {
  const { id = '' } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [usuario, setUsuario] = useState<UsuarioResponse | null>(null);
  const [nome, setNome] = useState('');
  const [email, setEmail] = useState('');
  const [perfil, setPerfil] = useState<PerfilUsuario>('Funcionario');
  const [erro, setErro] = useState<string | null>(null);
  const [sucesso, setSucesso] = useState<string | null>(null);
  const [salvando, setSalvando] = useState(false);
  const [alterandoStatus, setAlterandoStatus] = useState(false);
  const [modalStatusAberto, setModalStatusAberto] = useState(false);
  const [erroStatus, setErroStatus] = useState<string | null>(null);

  useEffect(() => {
    let cancelado = false;
    void (async () => {
      try {
        const u = await userService.getById(id);
        if (cancelado) return;
        setUsuario(u);
        setNome(u.nome);
        setEmail(u.email);
        setPerfil(u.perfil);
      } catch {
        if (!cancelado) setErro('Usuário não encontrado.');
      }
    })();
    return () => {
      cancelado = true;
    };
  }, [id]);

  const salvar = useCallback(async () => {
    if (!usuario) return;
    setSalvando(true);
    setErro(null);
    setSucesso(null);
    try {
      const atualizado = await userService.update(usuario.id, { nome, email, perfil });
      setUsuario(atualizado);
      setSucesso('Dados atualizados com sucesso.');
    } catch {
      setErro('Não foi possível atualizar o usuário. Verifique os campos e tente novamente.');
    } finally {
      setSalvando(false);
    }
  }, [usuario, nome, email, perfil]);

  const toggleStatus = useCallback(async () => {
    if (!usuario) return;
    const novoAtivo = !usuario.ativo;
    setAlterandoStatus(true);
    setErro(null);
    setErroStatus(null);
    setSucesso(null);
    try {
      await userService.updateStatus(usuario.id, novoAtivo);
      const refreshed = await userService.getById(usuario.id);
      setUsuario(refreshed);
      setModalStatusAberto(false);
      setSucesso(
        novoAtivo
          ? 'Usuário ativado com sucesso. O acesso ao sistema foi restaurado.'
          : 'Usuário inativado com sucesso. O acesso ao sistema foi bloqueado.',
      );
    } catch {
      // O modal segue aberto; o erro precisa aparecer DENTRO do dialog, pois o
      // restante da página fica aria-hidden enquanto o modal está aberto.
      setErroStatus('Não foi possível alterar o status do usuário.');
    } finally {
      setAlterandoStatus(false);
    }
  }, [usuario]);

  if (erro && !usuario) {
    return (
      <div className="px-8 py-8">
        <Button
          type="button"
          variant="outline"
          onClick={() => void navigate('/usuarios')}
          className="mb-4 h-9 rounded-full border-border bg-transparent px-4 text-sm"
        >
          <ArrowLeft className="mr-1 h-4 w-4" /> Voltar
        </Button>
        <p role="alert" className="text-sm text-red-400">
          {erro}
        </p>
      </div>
    );
  }

  if (!usuario) {
    return <div className="px-8 py-8 text-sm text-muted-foreground">Carregando…</div>;
  }

  const isBusy = salvando || alterandoStatus;

  return (
    <div className="space-y-6 px-8 py-8">
      <div className="flex items-center justify-between">
        <Button
          type="button"
          variant="outline"
          onClick={() => void navigate('/usuarios')}
          className="h-9 rounded-full border-border bg-transparent px-4 text-sm"
        >
          <ArrowLeft className="mr-1 h-4 w-4" /> Voltar
        </Button>
        <Button
          type="button"
          variant="outline"
          disabled={isBusy}
          onClick={() => {
            setErroStatus(null);
            setModalStatusAberto(true);
          }}
          className="h-9 rounded-full border-border bg-transparent px-4 text-sm hover:bg-accent hover:text-foreground text-muted-foreground"
        >
          {alterandoStatus ? (
            <Loader2 className="mr-1 h-4 w-4 animate-spin" />
          ) : (
            <Power className="mr-1 h-4 w-4" />
          )}
          {usuario.ativo ? 'Inativar usuário' : 'Ativar usuário'}
        </Button>
      </div>

      {/* ── Card principal: dados + status ── */}
      <Card className="border-border bg-card">
        <CardHeader>
          <CardTitle className="flex items-center gap-3 text-xl text-foreground">
            {usuario.nome}
            <span
              className={
                usuario.ativo
                  ? 'rounded-full bg-green-500/10 px-2 py-0.5 text-[10px] font-bold tracking-[0.15em] text-green-400'
                  : 'rounded-full bg-muted px-2 py-0.5 text-[10px] font-bold tracking-[0.15em] text-muted-foreground'
              }
            >
              {usuario.ativo ? 'ATIVO' : 'INATIVO'}
            </span>
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          {/* ── Campos de edição ── */}
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label
                htmlFor="detalhe-nome"
                className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground"
              >
                NOME
              </Label>
              <Input
                id="detalhe-nome"
                type="text"
                value={nome}
                onChange={(e) => setNome(e.target.value)}
                className="h-10 rounded-xl border-border bg-card text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0"
              />
            </div>
            <div className="space-y-1.5">
              <Label
                htmlFor="detalhe-email"
                className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground"
              >
                E-MAIL
              </Label>
              <Input
                id="detalhe-email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="h-10 rounded-xl border-border bg-card text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0"
              />
            </div>
            <div className="space-y-1.5">
              <Label
                htmlFor="detalhe-perfil"
                className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground"
              >
                PERFIL
              </Label>
              <select
                id="detalhe-perfil"
                value={perfil}
                onChange={(e) => setPerfil(e.target.value as PerfilUsuario)}
                className="h-10 w-full rounded-xl border border-border bg-card px-3 text-sm text-foreground focus-visible:ring-0"
              >
                {PERFIS.map((p) => (
                  <option key={p} value={p}>
                    {p}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* ── Separador visual ── */}
          <div className="border-t border-border" />

          {/* ── Controle de Status ── */}
          <div className="rounded-xl border border-border bg-background p-4">
            <div className="space-y-1.5">
              <Label
                htmlFor="detalhe-status-input"
                className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground"
              >
                STATUS DO USUÁRIO
              </Label>
              <Input
                id="detalhe-status-input"
                type="text"
                readOnly
                value={
                  usuario.ativo
                    ? 'Ativo — acesso ao sistema permitido'
                    : 'Inativo — acesso ao sistema bloqueado'
                }
                className={`h-10 rounded-xl border-border bg-card text-sm font-medium focus-visible:ring-0 ${
                  usuario.ativo ? 'text-green-400' : 'text-muted-foreground'
                }`}
              />
              <p className="text-[11px] text-muted-foreground">
                {usuario.ativo
                  ? 'Use o botão "Inativar usuário" acima para bloquear o login deste usuário.'
                  : 'Use o botão "Ativar usuário" acima para restaurar o acesso deste usuário ao sistema.'}
              </p>
            </div>
          </div>

          {/* ── Ações ── */}
          <div className="flex items-center justify-end">
            <Button
              type="button"
              disabled={isBusy}
              onClick={() => void salvar()}
              className="h-10 rounded-full bg-red-600 px-5 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 disabled:opacity-50"
            >
              <Save className="mr-1 h-4 w-4" />
              {salvando ? 'Salvando…' : 'Salvar alterações'}
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* ── Feedback: sucesso ── */}
      {sucesso && (
        <div
          role="status"
          aria-live="polite"
          className="flex items-start gap-3 rounded-xl border border-green-500/30 bg-green-950/30 px-4 py-3"
        >
          <span className="mt-0.5 h-4 w-4 shrink-0 text-green-500" aria-hidden="true">
            ✓
          </span>
          <p className="flex-1 text-sm font-medium text-green-400">{sucesso}</p>
          <button
            type="button"
            onClick={() => setSucesso(null)}
            aria-label="Fechar mensagem de sucesso"
            className="shrink-0 text-green-500/60 transition-colors hover:text-green-400"
          >
            <X className="h-4 w-4" aria-hidden="true" />
          </button>
        </div>
      )}

      {/* ── Feedback: erro ── */}
      {erro && (
        <div
          role="alert"
          aria-live="assertive"
          className="flex items-start gap-3 rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3"
        >
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-red-500" aria-hidden="true" />
          <p className="flex-1 text-sm font-medium text-red-400">{erro}</p>
          <button
            type="button"
            onClick={() => setErro(null)}
            aria-label="Fechar mensagem de erro"
            className="shrink-0 text-red-500/60 transition-colors hover:text-red-400"
          >
            <X className="h-4 w-4" aria-hidden="true" />
          </button>
        </div>
      )}

      {/* ── Modal de confirmação de ativação/inativação ── */}
      <Dialog
        open={modalStatusAberto}
        onOpenChange={(aberto) => {
          if (alterandoStatus) return;
          setModalStatusAberto(aberto);
          if (!aberto) setErroStatus(null);
        }}
      >
        <DialogContent className="sm:max-w-[425px] bg-background border-border">
          <DialogHeader>
            <DialogTitle className="text-foreground">
              {usuario.ativo ? 'Inativar usuário' : 'Ativar usuário'}
            </DialogTitle>
            <DialogDescription className="text-muted-foreground">
              {usuario.ativo
                ? `Deseja realmente inativar o usuário "${usuario.nome}"? O acesso dele ao sistema será bloqueado.`
                : `Deseja reativar o usuário "${usuario.nome}"? O acesso dele ao sistema será restaurado.`}
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
                setErroStatus(null);
              }}
              disabled={alterandoStatus}
              className="border-border bg-transparent text-foreground hover:bg-accent hover:text-foreground"
            >
              Cancelar
            </Button>
            <Button
              type="button"
              onClick={() => void toggleStatus()}
              disabled={alterandoStatus}
              className="bg-red-600 text-white hover:bg-red-700"
            >
              {alterandoStatus ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
              Confirmar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
