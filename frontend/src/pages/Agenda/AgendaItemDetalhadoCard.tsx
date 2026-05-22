import { CalendarClock, Car, Clock, FileText, Receipt, User } from 'lucide-react';

import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { formatarDuracao, formatarReais } from '@/lib/format';

import {
  classesStatus,
  formatarCpfCnpj,
  formatarFaixaHorario,
  formatarTelefone,
  rotuloStatus,
} from './agendaFormat';

import type { AgendaItemDetalhado } from '@/types/agenda';

interface AgendaItemDetalhadoCardProps {
  item: AgendaItemDetalhado;
}

/** Par rótulo/valor usado nos blocos de cliente e veículo. */
function Campo({ rotulo, valor }: { rotulo: string; valor: string }) {
  return (
    <div className="min-w-0">
      <dt className="text-[10px] font-bold uppercase tracking-[0.12em] text-zinc-400 dark:text-zinc-500">
        {rotulo}
      </dt>
      <dd className="truncate text-sm text-zinc-700 dark:text-zinc-200">{valor}</dd>
    </div>
  );
}

/**
 * Cartão completo da agenda no formato `detalhado` (RF009).
 *
 * <p>Mostra horário, status, cliente, veículo, serviços com preço/duração,
 * observações e totais. Layout responsivo: blocos empilham no mobile e formam
 * grade no desktop.</p>
 */
export function AgendaItemDetalhadoCard({ item }: AgendaItemDetalhadoCardProps) {
  return (
    <Card className="border border-zinc-200/70 dark:border-zinc-800/60">
      <CardHeader className="gap-3">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="flex items-center gap-2 text-sm font-semibold text-zinc-800 dark:text-zinc-100">
            <CalendarClock className="h-4 w-4 shrink-0 text-red-500" aria-hidden="true" />
            <span className="tabular-nums">{formatarFaixaHorario(item.inicio, item.fim)}</span>
          </div>
          <span
            className={`shrink-0 rounded-full px-2.5 py-1 text-[10px] font-bold tracking-[0.12em] ${classesStatus(
              item.status,
            )}`}
          >
            {rotuloStatus(item.status).toUpperCase()}
          </span>
        </div>
      </CardHeader>

      <CardContent className="flex flex-col gap-4">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          {/* Cliente */}
          <section
            aria-label="Dados do cliente"
            className="rounded-lg border border-zinc-200/60 bg-zinc-50/50 p-3 dark:border-zinc-800/40 dark:bg-zinc-950/30"
          >
            <h3 className="mb-2 flex items-center gap-1.5 text-xs font-semibold text-zinc-600 dark:text-zinc-300">
              <User className="h-3.5 w-3.5 text-red-500" aria-hidden="true" />
              Cliente
            </h3>
            <dl className="grid grid-cols-2 gap-2">
              <Campo rotulo="Nome" valor={item.cliente.nome} />
              <Campo rotulo="Documento" valor={formatarCpfCnpj(item.cliente.cpfCnpj)} />
              <Campo rotulo="Celular" valor={formatarTelefone(item.cliente.celular)} />
              <Campo rotulo="Telefone" valor={formatarTelefone(item.cliente.telefone)} />
            </dl>
          </section>

          {/* Veículo */}
          <section
            aria-label="Dados do veículo"
            className="rounded-lg border border-zinc-200/60 bg-zinc-50/50 p-3 dark:border-zinc-800/40 dark:bg-zinc-950/30"
          >
            <h3 className="mb-2 flex items-center gap-1.5 text-xs font-semibold text-zinc-600 dark:text-zinc-300">
              <Car className="h-3.5 w-3.5 text-red-500" aria-hidden="true" />
              Veículo
            </h3>
            <dl className="grid grid-cols-2 gap-2">
              <Campo rotulo="Placa" valor={item.veiculo.placa} />
              <Campo rotulo="Modelo" valor={item.veiculo.modelo} />
              <Campo rotulo="Fabricante" valor={item.veiculo.fabricante} />
              <Campo rotulo="Cor" valor={item.veiculo.cor} />
            </dl>
          </section>
        </div>

        {/* Serviços */}
        <section aria-label="Serviços do agendamento">
          <h3 className="mb-2 text-xs font-semibold text-zinc-600 dark:text-zinc-300">Serviços</h3>
          <ul className="divide-y divide-zinc-200/60 rounded-lg border border-zinc-200/60 dark:divide-zinc-800/40 dark:border-zinc-800/40">
            {item.servicos.map((servico) => (
              <li
                key={servico.id}
                className="flex flex-wrap items-center justify-between gap-2 px-3 py-2 text-sm"
              >
                <span className="min-w-0 truncate text-zinc-700 dark:text-zinc-200">
                  {servico.nome}
                </span>
                <span className="flex items-center gap-3 text-xs tabular-nums text-zinc-500 dark:text-zinc-400">
                  <span className="flex items-center gap-1">
                    <Clock className="h-3.5 w-3.5" aria-hidden="true" />
                    {formatarDuracao(servico.duracaoMin)}
                  </span>
                  <span className="font-medium text-zinc-700 dark:text-zinc-200">
                    {formatarReais(servico.preco)}
                  </span>
                </span>
              </li>
            ))}
          </ul>
        </section>

        {/* Observações */}
        {item.observacoes && (
          <section aria-label="Observações" className="flex items-start gap-2">
            <FileText className="mt-0.5 h-3.5 w-3.5 shrink-0 text-zinc-400" aria-hidden="true" />
            <p className="text-sm text-zinc-600 dark:text-zinc-300">{item.observacoes}</p>
          </section>
        )}

        {/* Totais */}
        <div className="flex flex-wrap items-center justify-end gap-x-6 gap-y-1 border-t border-zinc-200/60 pt-3 text-sm dark:border-zinc-800/40">
          <span className="flex items-center gap-1.5 text-zinc-500 dark:text-zinc-400">
            <Clock className="h-4 w-4" aria-hidden="true" />
            Duração total
            <strong className="ml-1 tabular-nums text-zinc-800 dark:text-zinc-100">
              {formatarDuracao(item.duracaoTotalMin)}
            </strong>
          </span>
          <span className="flex items-center gap-1.5 text-zinc-500 dark:text-zinc-400">
            <Receipt className="h-4 w-4" aria-hidden="true" />
            Valor total
            <strong className="ml-1 tabular-nums text-red-600 dark:text-red-400">
              {formatarReais(item.valorTotal)}
            </strong>
          </span>
        </div>
      </CardContent>
    </Card>
  );
}
