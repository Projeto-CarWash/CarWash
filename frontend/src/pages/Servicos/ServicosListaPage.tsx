import { Plus, Edit2, AlertCircle, Loader2, Power } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { servicoService, type Servico } from '@/services/servicoService';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';

export function ServicosListaPage() {
  const navigate = useNavigate();
  const [servicos, setServicos] = useState<Servico[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);
  
  const [servicoAtivacao, setServicoAtivacao] = useState<Servico | null>(null);
  const [modalAtivacaoAberto, setModalAtivacaoAberto] = useState(false);
  const [salvandoStatus, setSalvandoStatus] = useState(false);

  const carregarServicos = useCallback(async () => {
    setCarregando(true);
    setErro(null);
    try {
      const response = await servicoService.listar();
      if (!response || !Array.isArray(response.itens)) {
        throw new Error('Resposta inválida do servidor');
      }
      setServicos(response.itens);
    } catch {
      setErro('Não foi possível carregar a lista de serviços.');
    } finally {
      setCarregando(false);
    }
  }, []);

  useEffect(() => {
    void carregarServicos();
  }, [carregarServicos]);

  const abrirModalStatus = (servico: Servico) => {
    setServicoAtivacao(servico);
    setModalAtivacaoAberto(true);
  };

  const confirmarAlteracaoStatus = async () => {
    if (!servicoAtivacao) return;
    setSalvandoStatus(true);
    try {
      await servicoService.alterarStatus(servicoAtivacao.id, !servicoAtivacao.ativo);
      setServicos((prev) =>
        prev.map((s) =>
          s.id === servicoAtivacao.id ? { ...s, ativo: !s.ativo } : s
        )
      );
      setModalAtivacaoAberto(false);
    } catch {
      setErro('Não foi possível alterar o status do serviço.');
    } finally {
      setSalvandoStatus(false);
      setServicoAtivacao(null);
    }
  };

  return (
    <div className="flex h-full flex-col">
      <div className="flex items-center justify-between border-b border-zinc-800/60 bg-zinc-950/50 px-8 py-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-zinc-50">Serviços</h1>
          <p className="text-sm text-zinc-400">Catálogo de serviços oferecidos pelo CarWash.</p>
        </div>
        <Button
          type="button"
          onClick={() => void navigate('/servicos/novo')}
          className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 transition-colors"
        >
          <Plus className="mr-2 h-4 w-4" /> Novo serviço
        </Button>
      </div>

      <div className="flex-1 overflow-auto bg-zinc-950 p-8">
        {erro && (
          <div className="mb-6 flex items-start gap-3 rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3">
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-red-500" />
            <p className="text-sm font-medium text-red-400">{erro}</p>
          </div>
        )}

        <div className="rounded-xl border border-zinc-800/60 bg-zinc-900/30 shadow-xl overflow-hidden">
          <Table>
            <TableHeader className="bg-zinc-900/80">
              <TableRow className="border-zinc-800/60 hover:bg-transparent">
                <TableHead className="w-[300px] text-zinc-400">Nome</TableHead>
                <TableHead className="text-zinc-400">Preço</TableHead>
                <TableHead className="text-zinc-400">Duração</TableHead>
                <TableHead className="w-[120px] text-zinc-400">Status</TableHead>
                <TableHead className="w-[100px] text-right text-zinc-400">Ações</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {carregando ? (
                <TableRow>
                  <TableCell colSpan={5} className="h-32 text-center text-zinc-500">
                    <Loader2 className="mx-auto h-5 w-5 animate-spin mb-2" />
                    Carregando serviços...
                  </TableCell>
                </TableRow>
              ) : servicos.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="h-32 text-center text-zinc-500">
                    Nenhum serviço cadastrado.
                  </TableCell>
                </TableRow>
              ) : (
                servicos.map((servico) => (
                  <TableRow
                    key={servico.id}
                    className="border-zinc-800/40 hover:bg-zinc-800/30 transition-colors"
                  >
                    <TableCell className="font-medium text-zinc-200">
                      {servico.nome}
                    </TableCell>
                    <TableCell className="text-zinc-300">
                      {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(servico.preco)}
                    </TableCell>
                    <TableCell className="text-zinc-300">
                      {servico.duracaoMin} min
                    </TableCell>
                    <TableCell>
                      <span
                        className={`inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider ${
                          servico.ativo
                            ? 'bg-green-500/10 text-green-400'
                            : 'bg-zinc-800/50 text-zinc-500'
                        }`}
                      >
                        {servico.ativo ? 'Ativo' : 'Inativo'}
                      </span>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => void navigate(`/servicos/${servico.id}/editar`)}
                          className="h-8 w-8 text-zinc-400 hover:bg-zinc-800 hover:text-zinc-100"
                        >
                          <Edit2 className="h-4 w-4" />
                          <span className="sr-only">Editar</span>
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => abrirModalStatus(servico)}
                          className="h-8 w-8 text-zinc-400 hover:bg-zinc-800 hover:text-zinc-100"
                        >
                          <Power className="h-4 w-4" />
                          <span className="sr-only">
                            {servico.ativo ? 'Desativar' : 'Ativar'}
                          </span>
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
      </div>

      <Dialog open={modalAtivacaoAberto} onOpenChange={setModalAtivacaoAberto}>
        <DialogContent className="sm:max-w-[425px] bg-zinc-950 border-zinc-800">
          <DialogHeader>
            <DialogTitle className="text-zinc-100">
              {servicoAtivacao?.ativo ? 'Desativar serviço' : 'Ativar serviço'}
            </DialogTitle>
            <DialogDescription className="text-zinc-400">
              {servicoAtivacao?.ativo
                ? `Deseja realmente desativar o serviço "${servicoAtivacao?.nome}"? Ele não estará mais disponível para novos agendamentos.`
                : `Deseja reativar o serviço "${servicoAtivacao?.nome}"? Ele voltará a estar disponível no catálogo.`}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="mt-4 gap-2 sm:gap-0">
            <Button
              type="button"
              variant="outline"
              onClick={() => setModalAtivacaoAberto(false)}
              disabled={salvandoStatus}
              className="border-zinc-700 bg-transparent text-zinc-300 hover:bg-zinc-800 hover:text-zinc-100"
            >
              Cancelar
            </Button>
            <Button
              type="button"
              onClick={confirmarAlteracaoStatus}
              disabled={salvandoStatus}
              className="bg-red-600 text-white hover:bg-red-700"
            >
              {salvandoStatus ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
              Confirmar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
