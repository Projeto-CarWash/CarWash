import {
  Car,
  ChevronDown,
  ChevronUp,
  Clock,
  Eye,
  MapPin,
  UserCircle,
  Wrench,
} from 'lucide-react';
import { useState } from 'react';

import { Button } from '@/components/ui/button';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';

import { classesStatus, formatarData, formatarHora, rotuloStatus } from './historicoFormat';

import type { HistoricoItem } from '@/types/historico';

interface HistoricoListaProps {
  itens: HistoricoItem[];
  onVerDetalhe: (item: HistoricoItem) => void;
}

/** Monta o resumo dos serviços (nome principal + "+N"). */
function resumoServicos(item: HistoricoItem): string {
  if (!item.servicos || item.servicos.length === 0) return '—';
  const primeiro = item.servicos[0]!.nome;
  if (item.servicos.length === 1) return primeiro;
  return `${primeiro} +${item.servicos.length - 1}`;
}

/**
 * Lista de resultados do histórico de atendimentos (RF012).
 *
 * <p>Desktop: tabela responsiva com todas as colunas visíveis.</p>
 * <p>Mobile: cards compactos com Data/Status/Placa visíveis e expansão para
 * dados adicionais.</p>
 */
export function HistoricoLista({ itens, onVerDetalhe }: HistoricoListaProps) {
  return (
    <>
      {/* Desktop — Tabela */}
      <div className="hidden md:block">
        <HistoricoTabela itens={itens} onVerDetalhe={onVerDetalhe} />
      </div>

      {/* Mobile — Cards */}
      <div className="block md:hidden">
        <HistoricoCards itens={itens} onVerDetalhe={onVerDetalhe} />
      </div>
    </>
  );
}

/** Tabela desktop. */
function HistoricoTabela({ itens, onVerDetalhe }: HistoricoListaProps) {
  return (
    <div className="rounded-2xl border border-zinc-800/60 bg-zinc-900/30">
      <Table>
        <TableHeader>
          <TableRow className="border-zinc-800/60 hover:bg-transparent">
            <TableHead className="text-zinc-400">Data</TableHead>
            <TableHead className="text-zinc-400">Início</TableHead>
            <TableHead className="text-zinc-400">Término</TableHead>
            <TableHead className="text-zinc-400">Status</TableHead>
            <TableHead className="text-zinc-400">Placa</TableHead>
            <TableHead className="text-zinc-400">Serviços</TableHead>
            <TableHead className="text-zinc-400">Responsável</TableHead>
            <TableHead className="text-right text-zinc-400">Ação</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {itens.map((item) => (
            <TableRow
              key={item.agendamentoId}
              className="border-zinc-800/40 transition-colors hover:bg-zinc-800/20"
            >
              <TableCell className="text-sm font-medium text-zinc-200">
                {formatarData(item.inicio)}
              </TableCell>
              <TableCell className="text-sm text-zinc-300">
                {formatarHora(item.inicio)}
              </TableCell>
              <TableCell className="text-sm text-zinc-300">
                {formatarHora(item.fim)}
              </TableCell>
              <TableCell>
                <span
                  className={`inline-flex rounded-full px-2.5 py-1 text-[10px] font-bold tracking-[0.12em] ${classesStatus(item.status)}`}
                >
                  {rotuloStatus(item.status).toUpperCase()}
                </span>
              </TableCell>
              <TableCell className="font-mono text-sm font-semibold text-zinc-200">
                {item.veiculo.placa}
              </TableCell>
              <TableCell className="max-w-[200px] truncate text-sm text-zinc-400">
                {resumoServicos(item)}
              </TableCell>
              <TableCell className="text-sm text-zinc-400">
                {item.cliente.nome ?? '—'}
              </TableCell>
              <TableCell className="text-right">
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => onVerDetalhe(item)}
                  className="text-red-400 hover:text-red-300"
                  aria-label={`Ver detalhe do atendimento de ${formatarData(item.inicio)}`}
                >
                  <Eye className="mr-1 h-3.5 w-3.5" aria-hidden="true" />
                  Ver detalhe
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

/** Cards mobile. */
function HistoricoCards({ itens, onVerDetalhe }: HistoricoListaProps) {
  return (
    <ul className="space-y-3" aria-label="Histórico de atendimentos">
      {itens.map((item) => (
        <HistoricoCardItem key={item.agendamentoId} item={item} onVerDetalhe={onVerDetalhe} />
      ))}
    </ul>
  );
}

/** Card individual mobile. */
function HistoricoCardItem({
  item,
  onVerDetalhe,
}: {
  item: HistoricoItem;
  onVerDetalhe: (item: HistoricoItem) => void;
}) {
  const [expandido, setExpandido] = useState(false);

  return (
    <li className="rounded-xl border border-zinc-800/60 bg-zinc-900/30 p-4 transition-colors">
      {/* Linha principal — sempre visível */}
      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-3 overflow-hidden">
          <div className="min-w-0">
            <p className="text-sm font-semibold text-zinc-200">{formatarData(item.inicio)}</p>
            <p className="font-mono text-xs font-semibold text-zinc-400">{item.veiculo.placa}</p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <span
            className={`rounded-full px-2 py-0.5 text-[10px] font-bold tracking-[0.12em] ${classesStatus(item.status)}`}
          >
            {rotuloStatus(item.status).toUpperCase()}
          </span>
          <button
            type="button"
            onClick={() => setExpandido(!expandido)}
            className="rounded-lg p-1.5 text-zinc-400 transition-colors hover:bg-zinc-800 hover:text-zinc-200"
            aria-expanded={expandido}
            aria-label={expandido ? 'Recolher detalhes' : 'Expandir detalhes'}
          >
            {expandido ? (
              <ChevronUp className="h-4 w-4" aria-hidden="true" />
            ) : (
              <ChevronDown className="h-4 w-4" aria-hidden="true" />
            )}
          </button>
        </div>
      </div>

      {/* Dados expandidos */}
      {expandido && (
        <div className="mt-3 space-y-2 border-t border-zinc-800/40 pt-3">
          <div className="flex items-center gap-2 text-xs text-zinc-400">
            <Clock className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
            <span>
              {formatarHora(item.inicio)} — {formatarHora(item.fim)}
            </span>
          </div>
          <div className="flex items-center gap-2 text-xs text-zinc-400">
            <Wrench className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
            <span className="truncate">{resumoServicos(item)}</span>
          </div>
          <div className="flex items-center gap-2 text-xs text-zinc-400">
            <Car className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
            <span>
              {item.veiculo.modelo} • {item.veiculo.cor}
            </span>
          </div>
          <div className="flex items-center gap-2 text-xs text-zinc-400">
            <MapPin className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
            <span>{item.filialId}</span>
          </div>
          {item.cliente.nome && (
            <div className="flex items-center gap-2 text-xs text-zinc-400">
              <UserCircle className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
              <span>{item.cliente.nome}</span>
            </div>
          )}
          <div className="pt-1">
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => onVerDetalhe(item)}
              className="w-full text-red-400 hover:text-red-300"
              aria-label={`Ver detalhe do atendimento de ${formatarData(item.inicio)}`}
            >
              <Eye className="mr-1 h-3.5 w-3.5" aria-hidden="true" />
              Ver detalhe
            </Button>
          </div>
        </div>
      )}
    </li>
  );
}
