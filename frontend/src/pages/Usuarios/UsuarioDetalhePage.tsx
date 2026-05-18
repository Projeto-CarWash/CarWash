import { ArrowLeft, Power, Save } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
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
    setSalvando(true);
    setErro(null);
    try {
      await userService.updateStatus(usuario.id, !usuario.ativo);
      const refreshed = await userService.getById(usuario.id);
      setUsuario(refreshed);
    } catch {
      setErro('Não foi possível alterar o status do usuário.');
    } finally {
      setSalvando(false);
    }
  }, [usuario]);

  if (erro && !usuario) {
    return (
      <div className="px-8 py-8">
        <Button
          type="button"
          variant="outline"
          onClick={() => void navigate('/usuarios')}
          className="mb-4 h-9 rounded-full border-zinc-700/60 bg-transparent px-4 text-sm"
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
    return <div className="px-8 py-8 text-sm text-zinc-500">Carregando…</div>;
  }

  return (
    <div className="space-y-6 px-8 py-8">
      <div className="flex items-center justify-between">
        <Button
          type="button"
          variant="outline"
          onClick={() => void navigate('/usuarios')}
          className="h-9 rounded-full border-zinc-700/60 bg-transparent px-4 text-sm"
        >
          <ArrowLeft className="mr-1 h-4 w-4" /> Voltar
        </Button>
        <Button
          type="button"
          variant="outline"
          disabled={salvando}
          onClick={() => void toggleStatus()}
          className="h-9 rounded-full border-zinc-700/60 bg-transparent px-4 text-sm"
        >
          <Power className="mr-1 h-4 w-4" />
          {usuario.ativo ? 'Inativar usuário' : 'Reativar usuário'}
        </Button>
      </div>

      <Card className="border-zinc-800/60 bg-zinc-900/30">
        <CardHeader>
          <CardTitle className="flex items-center gap-3 text-xl text-zinc-100">
            {usuario.nome}
            <span
              className={
                usuario.ativo
                  ? 'rounded-full bg-green-500/10 px-2 py-0.5 text-[10px] font-bold tracking-[0.15em] text-green-400'
                  : 'rounded-full bg-zinc-700/40 px-2 py-0.5 text-[10px] font-bold tracking-[0.15em] text-zinc-500'
              }
            >
              {usuario.ativo ? 'ATIVO' : 'INATIVO'}
            </span>
          </CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="space-y-1.5">
            <Label htmlFor="nome" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
              NOME
            </Label>
            <Input
              id="nome"
              type="text"
              value={nome}
              onChange={(e) => setNome(e.target.value)}
              className="h-10 rounded-xl border-zinc-700/60 bg-zinc-900/50 text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0"
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="email" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
              E-MAIL
            </Label>
            <Input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="h-10 rounded-xl border-zinc-700/60 bg-zinc-900/50 text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0"
            />
          </div>
          <div className="space-y-1.5">
            <Label
              htmlFor="perfil"
              className="text-[10px] font-bold tracking-[0.2em] text-zinc-500"
            >
              PERFIL
            </Label>
            <select
              id="perfil"
              value={perfil}
              onChange={(e) => setPerfil(e.target.value as PerfilUsuario)}
              className="h-10 w-full rounded-xl border border-zinc-700/60 bg-zinc-900/50 px-3 text-sm text-zinc-200 focus-visible:ring-0"
            >
              {PERFIS.map((p) => (
                <option key={p} value={p}>
                  {p}
                </option>
              ))}
            </select>
          </div>
          <div className="flex items-end justify-end">
            <Button
              type="button"
              disabled={salvando}
              onClick={() => void salvar()}
              className="h-10 rounded-full bg-red-600 px-5 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 disabled:opacity-50"
            >
              <Save className="mr-1 h-4 w-4" />
              {salvando ? 'Salvando…' : 'Salvar alterações'}
            </Button>
          </div>
        </CardContent>
      </Card>

      {sucesso && (
        <p role="status" className="text-sm text-green-400">
          {sucesso}
        </p>
      )}
      {erro && (
        <p role="alert" className="text-sm text-red-400">
          {erro}
        </p>
      )}
    </div>
  );
}
