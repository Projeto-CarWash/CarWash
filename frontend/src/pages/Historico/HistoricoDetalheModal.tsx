import { X } from 'lucide-react';
import { useEffect } from 'react';

import { formatarReais } from '@/lib/format';

import {
  classesStatus,
  formatarData,
  formatarFaixaHorario,
  formatarHora,
  rotuloStatus,
} from './historicoFormat';

import type { HistoricoItem } from '@/types/historico';

interface HistoricoDetalheModalProps {
  item: HistoricoItem;
  onFechar: () => void;
}

/**
 * Modal de detalhe do atendimento (RF012).
 *
 * <p>Exibe todos os dados retornados pela API: cliente, veículo, serviços,
 * horários, status, valor, duração. Inclui observações logísticas quando
 * existirem (integração RF011) e status consistente com a lista (RF010).</p>
 */
export function HistoricoDetalheModal({ item, onFechar }: HistoricoDetalheModalProps) {
  // Fecha com Escape
  useEffect(() => {
    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        onFechar();
      }
    }
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onFechar]);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <button
        type="button"
        className="absolute inset-0 h-full w-full cursor-default border-none bg-black/60 outline-none backdrop-blur-sm focus:outline-none"
        aria-label="Fechar detalhe"
        onClick={onFechar}
        tabIndex={-1}
      />

      {/* Dialog */}
      <div
        className="relative z-10 mx-4 max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-2xl border border-zinc-800/60 bg-zinc-900 p-6 shadow-2xl"
        role="dialog"
        aria-modal="true"
        aria-label="Detalhe do atendimento"
      >
        {/* Fechar */}
        <button
          type="button"
          onClick={onFechar}
          className="absolute right-4 top-4 rounded-full p-1 text-zinc-400 transition-colors hover:bg-zinc-800 hover:text-zinc-200"
          aria-label="Fechar detalhe"
        >
          <X className="h-5 w-5" aria-hidden="true" />
        </button>

        {/* Cabeçalho */}
        <div className="mb-4 flex items-center justify-between border-b border-zinc-800/40 pb-3">
          <h2 className="text-lg font-bold text-zinc-50">Detalhe do Atendimento</h2>
          <span
            className={`rounded-full px-2.5 py-1 text-[10px] font-bold tracking-[0.12em] ${classesStatus(item.status)}`}
          >
            {rotuloStatus(item.status).toUpperCase()}
          </span>
        </div>

        {/* Conteúdo */}
        <div className="space-y-4">
          {/* Horário */}
          <Secao titulo="Horário">
            <div className="grid grid-cols-2 gap-3">
              <Campo rotulo="Data" valor={formatarData(item.inicio)} />
              <Campo rotulo="Faixa" valor={formatarFaixaHorario(item.inicio, item.fim)} />
              <Campo rotulo="Início" valor={formatarHora(item.inicio)} />
              <Campo rotulo="Término" valor={formatarHora(item.fim)} />
              <Campo rotulo="Duração" valor={`${item.duracaoTotalMin} min`} />
              <Campo rotulo="Valor Total" valor={formatarReais(item.valorTotal)} />
            </div>
          </Secao>

          {/* Cliente */}
          <Secao titulo="Cliente">
            <div className="grid grid-cols-2 gap-3">
              <Campo rotulo="Nome" valor={item.cliente.nome} />
              <Campo rotulo="Documento" valor={item.cliente.cpfCnpj} />
              <Campo rotulo="Celular" valor={item.cliente.celular} />
              {item.cliente.telefone && <Campo rotulo="Telefone" valor={item.cliente.telefone} />}
            </div>
          </Secao>

          {/* Veículo */}
          <Secao titulo="Veículo">
            <div className="grid grid-cols-2 gap-3">
              <Campo rotulo="Placa" valor={item.veiculo.placa} />
              <Campo rotulo="Modelo" valor={item.veiculo.modelo} />
              <Campo rotulo="Fabricante" valor={item.veiculo.fabricante} />
              <Campo rotulo="Cor" valor={item.veiculo.cor} />
            </div>
          </Secao>

          {/* Serviços */}
          <Secao titulo="Serviços">
            <ul className="space-y-2">
              {item.servicos.map((s) => (
                <li
                  key={s.id}
                  className="flex items-center justify-between rounded-lg border border-zinc-800/40 bg-zinc-950/30 px-3 py-2"
                >
                  <div>
                    <p className="text-sm font-medium text-zinc-200">{s.nome}</p>
                    <p className="text-xs text-zinc-500">{s.duracaoMin} min</p>
                  </div>
                  <span className="text-sm font-semibold text-zinc-300">
                    {formatarReais(s.preco)}
                  </span>
                </li>
              ))}
            </ul>
          </Secao>

          {/* Observações */}
          {item.observacoes && (
            <Secao titulo="Observações">
              <p className="text-sm text-zinc-300">{item.observacoes}</p>
            </Secao>
          )}

          {/* Observações logísticas (RF011) */}
          {item.observacoesLogisticas && (
            <Secao titulo="Observações Logísticas">
              <p className="text-sm text-zinc-300">{item.observacoesLogisticas}</p>
            </Secao>
          )}

          {/* Metadados */}
          <div className="border-t border-zinc-800/40 pt-3">
            <div className="flex flex-wrap gap-x-6 gap-y-1 text-[10px] text-zinc-500">
              <span>Criado em: {formatarData(item.criadoEm)}</span>
              <span>Atualizado em: {formatarData(item.atualizadoEm)}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

/** Seção com título. */
function Secao({ titulo, children }: { titulo: string; children: React.ReactNode }) {
  return (
    <div className="rounded-xl border border-zinc-800/40 bg-zinc-950/30 p-4">
      <p className="mb-2 text-[10px] font-bold uppercase tracking-[0.12em] text-zinc-500">
        {titulo}
      </p>
      {children}
    </div>
  );
}

/** Campo rotulado. */
function Campo({ rotulo, valor }: { rotulo: string; valor: string }) {
  return (
    <div>
      <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-zinc-500">{rotulo}</p>
      <p className="mt-0.5 text-sm text-zinc-200">{valor}</p>
    </div>
  );
}
