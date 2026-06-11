import { useQuery, useQueryClient } from '@tanstack/react-query';
import { AlertCircle, CheckCircle2, Loader2, Plus, RefreshCw, Users } from 'lucide-react';
import { useState } from 'react';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { maskCelular, maskCpfCnpj } from '@/lib/masks';
import { responsavelService } from '@/services/responsavelService';
import { VINCULO_LABELS } from '@/types/responsavel';

import { ResponsavelModal } from './ResponsavelModal';


import type { Responsavel } from '@/types/responsavel';

interface ResponsaveisSectionProps {
  clienteId: string;
}

export function ResponsaveisSection({ clienteId }: ResponsaveisSectionProps) {
  const queryClient = useQueryClient();
  const [modalOpen, setModalOpen] = useState(false);
  const [sucessoMsg, setSucessoMsg] = useState<string | null>(null);

  // Fetch list of responsibles
  const {
    data: responsaveis,
    isLoading,
    isError,
    refetch,
  } = useQuery<Responsavel[]>({
    queryKey: ['clientes', clienteId, 'responsaveis'],
    queryFn: () => responsavelService.listarPorCliente(clienteId),
    staleTime: 5000,
  });

  const handleCreateSuccess = (newResp: Responsavel) => {
    // Invalidate the query key to trigger automatic list refresh
    void queryClient.invalidateQueries({
      queryKey: ['clientes', clienteId, 'responsaveis'],
    });

    setSucessoMsg(`Responsável "${newResp.nome}" cadastrado com sucesso!`);
    setTimeout(() => setSucessoMsg(null), 4000);
  };

  return (
    <div className="space-y-4">
      {sucessoMsg && (
        <div
          role="status"
          aria-live="polite"
          className="flex items-center gap-2 rounded-xl border border-green-200 dark:border-green-500/30 bg-green-50 dark:bg-green-950/30 px-4 py-3 text-sm text-green-600 dark:text-green-400 animate-fadeIn"
        >
          <CheckCircle2 className="h-4 w-4 shrink-0" aria-hidden="true" />
          <span>{sucessoMsg}</span>
        </div>
      )}

      <Card className="border border-border dark:border-zinc-800/60 bg-white dark:bg-zinc-900/30">
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-4 border-b border-border dark:border-zinc-800/60">
          <div>
            <CardTitle className="text-lg text-foreground dark:text-zinc-100 flex items-center gap-2">
              <Users className="h-5 w-5 text-red-500" />
              Responsáveis
            </CardTitle>
            <CardDescription className="text-muted-foreground dark:text-zinc-400">
              Contatos autorizados ou representantes legais deste cliente.
            </CardDescription>
          </div>
          <Button
            onClick={() => setModalOpen(true)}
            className="h-9 rounded-full bg-red-600 px-4 text-xs font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 transition-colors"
          >
            <Plus className="mr-1 h-3.5 w-3.5" />
            Adicionar responsável
          </Button>
        </CardHeader>

        <CardContent className="p-0">
          {isLoading ? (
            <div
              className="flex items-center justify-center py-10 gap-2 text-sm text-muted-foreground"
              aria-live="polite"
            >
              <Loader2 className="h-4 w-4 animate-spin" />
              Carregando responsáveis…
            </div>
          ) : isError ? (
            <div className="flex flex-col items-center justify-center py-8 px-4 text-center gap-3">
              <AlertCircle className="h-6 w-6 text-red-500" />
              <p className="text-sm text-muted-foreground">
                Não foi possível carregar os responsáveis para este cliente.
              </p>
              <Button
                type="button"
                variant="outline"
                onClick={() => void refetch()}
                className="h-8 rounded-full border-border dark:border-zinc-700/60 bg-transparent px-4 text-xs text-muted-foreground dark:text-zinc-300 hover:bg-accent dark:hover:bg-muted"
              >
                <RefreshCw className="mr-1.5 h-3.5 w-3.5" />
                Tentar novamente
              </Button>
            </div>
          ) : !responsaveis || responsaveis.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-10 px-4 text-center gap-2">
              <p className="text-sm text-muted-foreground">Nenhum responsável vinculado a este cliente.</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow className="border-b border-border dark:border-zinc-800/60">
                    <TableHead className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground uppercase">
                      Nome
                    </TableHead>
                    <TableHead className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground uppercase">
                      Documento
                    </TableHead>
                    <TableHead className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground uppercase">
                      Grau de Vínculo
                    </TableHead>
                    <TableHead className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground uppercase">
                      Telefone
                    </TableHead>
                    <TableHead className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground uppercase">
                      E-mail
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {responsaveis.map((resp) => (
                    <TableRow
                      key={resp.id}
                      className="border-b border-border dark:border-zinc-850/30 hover:bg-accent dark:hover:bg-background"
                    >
                      <TableCell className="text-sm font-semibold text-foreground dark:text-zinc-200">
                        {resp.nome}
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {maskCpfCnpj(resp.documento)}
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {VINCULO_LABELS[resp.grauVinculo] ?? resp.grauVinculo}
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {resp.telefone ? maskCelular(resp.telefone) : '—'}
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">{resp.email ?? '—'}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>

      <ResponsavelModal
        open={modalOpen}
        onOpenChange={setModalOpen}
        clienteId={clienteId}
        onSuccess={handleCreateSuccess}
      />
    </div>
  );
}
