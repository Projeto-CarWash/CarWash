import { AlertCircle, Building2, Edit2, Loader2, Plus, RefreshCw } from 'lucide-react';
import { useMemo } from 'react';
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
import { useFiliaisLista } from '@/hooks/useFilialQueries';

/**
 * Tela de gerência de filiais (RF017/RF018).
 *
 * <p>Lista todas as filiais com status operacional e dá acesso ao cadastro e à
 * edição. A listagem usa TanStack Query e é invalidada automaticamente após
 * cadastro/edição — sem necessidade de refresh manual.</p>
 */
export function FiliaisListaPage() {
  const navigate = useNavigate();
  const { data, isLoading, isError, refetch, isRefetching } = useFiliaisLista();

  const filiais = useMemo(() => data?.itens ?? [], [data]);

  return (
    <div className="flex h-full flex-col">
      <div className="flex items-center justify-between border-b border-zinc-800/60 bg-zinc-950/50 px-8 py-6">
        <div className="flex items-center gap-3">
          <span
            className="flex h-10 w-10 items-center justify-center rounded-lg bg-red-600/10 text-red-500"
            aria-hidden="true"
          >
            <Building2 className="h-5 w-5" />
          </span>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-zinc-50">Filiais</h1>
            <p className="text-sm text-zinc-400">
              Unidades operacionais disponíveis para agendamento.
            </p>
          </div>
        </div>
        <Button
          type="button"
          onClick={() => void navigate('/filiais/nova')}
          className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 transition-colors hover:bg-red-700"
        >
          <Plus className="mr-2 h-4 w-4" /> Nova filial
        </Button>
      </div>

      <div className="flex-1 overflow-auto bg-zinc-950 p-8">
        {isError && (
          <div
            role="alert"
            className="mb-6 flex items-start gap-3 rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3"
          >
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-red-500" aria-hidden="true" />
            <div className="flex-1">
              <p className="text-sm font-medium text-red-400">
                Não foi possível carregar a lista de filiais.
              </p>
              <Button
                type="button"
                variant="outline"
                onClick={() => void refetch()}
                className="mt-2 h-8 rounded-full border-red-500/30 bg-transparent px-4 text-xs text-red-400 hover:bg-red-950/30"
              >
                <RefreshCw className="mr-1 h-3 w-3" /> Tentar novamente
              </Button>
            </div>
          </div>
        )}

        <div className="overflow-hidden rounded-xl border border-zinc-800/60 bg-zinc-900/30 shadow-xl">
          <Table>
            <TableHeader className="bg-zinc-900/80">
              <TableRow className="border-zinc-800/60 hover:bg-transparent">
                <TableHead className="w-[260px] text-zinc-400">Nome</TableHead>
                <TableHead className="text-zinc-400">Código</TableHead>
                <TableHead className="text-zinc-400">Cidade</TableHead>
                <TableHead className="w-[80px] text-zinc-400">UF</TableHead>
                <TableHead className="w-[120px] text-zinc-400">Status</TableHead>
                <TableHead className="w-[80px] text-right text-zinc-400">Ações</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={6} className="h-32 text-center text-zinc-500">
                    <Loader2 className="mx-auto mb-2 h-5 w-5 animate-spin" aria-hidden="true" />
                    Carregando filiais...
                  </TableCell>
                </TableRow>
              ) : filiais.length === 0 && !isError ? (
                <TableRow>
                  <TableCell colSpan={6} className="h-32 text-center text-zinc-500">
                    Nenhuma filial cadastrada.
                  </TableCell>
                </TableRow>
              ) : (
                filiais.map((filial) => (
                  <TableRow
                    key={filial.id}
                    className="border-zinc-800/40 transition-colors hover:bg-zinc-800/30"
                  >
                    <TableCell className="font-medium text-zinc-200">{filial.nome}</TableCell>
                    <TableCell className="font-mono text-zinc-300">
                      {filial.codigo ?? '—'}
                    </TableCell>
                    <TableCell className="text-zinc-300">{filial.cidade ?? '—'}</TableCell>
                    <TableCell className="text-zinc-300">{filial.uf ?? '—'}</TableCell>
                    <TableCell>
                      <span
                        className={`inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider ${
                          filial.ativo
                            ? 'bg-green-500/10 text-green-400'
                            : 'bg-zinc-800/50 text-zinc-500'
                        }`}
                      >
                        {filial.ativo ? 'Ativa' : 'Inativa'}
                      </span>
                    </TableCell>
                    <TableCell className="text-right">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => void navigate(`/filiais/${filial.id}/editar`)}
                        className="h-8 w-8 text-zinc-400 hover:bg-zinc-800 hover:text-zinc-100"
                      >
                        <Edit2 className="h-4 w-4" />
                        <span className="sr-only">Editar {filial.nome}</span>
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>

        {isRefetching && (
          <p className="mt-3 flex items-center gap-2 text-xs text-zinc-500" aria-live="polite">
            <Loader2 className="h-3 w-3 animate-spin" aria-hidden="true" /> Atualizando lista...
          </p>
        )}
      </div>
    </div>
  );
}
