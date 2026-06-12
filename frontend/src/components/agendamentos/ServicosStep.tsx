import {
  AlertCircle,
  ChevronLeft,
  ChevronRight,
  Clock,
  DollarSign,
  RefreshCw,
  Wrench,
} from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';

import { Button } from '@/components/ui/button';
import { agendamentoService } from '@/services/agendamentoService';

import type { ServicoAtivo } from '@/types/agendamento';
function formatarPreco(valor: number): string {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

function formatarDuracao(minutos: number): string {
  if (minutos < 60) return `${minutos} min`;
  const h = Math.floor(minutos / 60);
  const m = minutos % 60;
  return m > 0 ? `${h}h ${m}min` : `${h}h`;
}

interface ServicosStepProps {
  servicosSelecionados: ServicoAtivo[];
  onServicosChange: (servicos: ServicoAtivo[]) => void;
  onNext: () => void;
  onBack: () => void;
}

export function ServicosStep({
  servicosSelecionados,
  onServicosChange,
  onNext,
  onBack,
}: ServicosStepProps) {
  const [catalogo, setCatalogo] = useState<ServicoAtivo[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);
  const [tentouAvancar, setTentouAvancar] = useState(false);

  const carregarCatalogo = useCallback(() => {
    setCarregando(true);
    setErro(null);
    agendamentoService
      .listarServicosAtivos()
      .then((s) => setCatalogo(s))
      .catch(() => setErro('Não foi possível carregar o catálogo de serviços. Tente novamente.'))
      .finally(() => setCarregando(false));
  }, []);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    carregarCatalogo();
  }, [carregarCatalogo]);

  const handleToggleServico = useCallback(
    (servico: ServicoAtivo) => {
      const exists = servicosSelecionados.some((s) => s.id === servico.id);
      if (exists) {
        onServicosChange(servicosSelecionados.filter((s) => s.id !== servico.id));
      } else {
        onServicosChange([...servicosSelecionados, servico]);
      }
      setTentouAvancar(false);
    },
    [servicosSelecionados, onServicosChange],
  );

  const handleNext = useCallback(() => {
    setTentouAvancar(true);
    if (servicosSelecionados.length > 0) {
      onNext();
    }
  }, [servicosSelecionados.length, onNext]);

  const duracaoTotal = servicosSelecionados.reduce((sum, s) => sum + s.duracao, 0);
  const valorTotal = servicosSelecionados.reduce((sum, s) => sum + s.preco, 0);

  return (
    <div>
      <div className="mb-5">
        <h3 className="text-xl font-semibold text-foreground">Serviços</h3>
        <p className="mt-1 text-sm text-muted-foreground">
          Selecione os serviços que serão realizados neste agendamento.
        </p>
      </div>

      {erro && (
        <div className="mb-4 rounded-xl border border-red-500/30 bg-red-950/20 px-4 py-3">
          <p className="text-sm text-red-400">{erro}</p>
          <Button
            type="button"
            variant="outline"
            onClick={carregarCatalogo}
            className="mt-2 h-8 rounded-full border-red-500/30 bg-transparent px-4 text-xs text-red-400 hover:bg-red-950/30"
          >
            <RefreshCw className="mr-1 h-3 w-3" /> Tentar novamente
          </Button>
        </div>
      )}

      {carregando && (
        <div className="flex items-center gap-2 rounded-xl border border-border bg-card px-4 py-8 text-sm text-muted-foreground">
          <RefreshCw className="h-4 w-4 animate-spin" />
          Carregando catálogo de serviços…
        </div>
      )}

      {!carregando && !erro && (
        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
          {catalogo.map((servico) => {
            const selected = servicosSelecionados.some((s) => s.id === servico.id);
            return (
              <button
                key={servico.id}
                type="button"
                onClick={() => handleToggleServico(servico)}
                className={`group relative flex flex-col gap-2 rounded-xl border px-4 py-3 text-left transition-all ${
                  selected
                    ? 'border-red-500/50 bg-red-950/20 shadow-[0_0_0_1px_rgba(239,68,68,0.3)]'
                    : 'border-border bg-card hover:border-ring hover:bg-accent'
                }`}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="flex items-center gap-2">
                    <div
                      className={`flex h-8 w-8 items-center justify-center rounded-lg ${
                        selected ? 'bg-red-600/20' : 'bg-muted'
                      }`}
                    >
                      <Wrench
                        className={`h-3.5 w-3.5 ${selected ? 'text-red-500' : 'text-muted-foreground'}`}
                      />
                    </div>
                    <p
                      className={`text-sm font-medium ${selected ? 'text-foreground' : 'text-foreground'}`}
                    >
                      {servico.nome}
                    </p>
                  </div>
                  {selected && (
                    <div className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-red-600">
                      <svg
                        className="h-3 w-3 text-foreground"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                        strokeWidth={3}
                      >
                        <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                      </svg>
                    </div>
                  )}
                </div>
                {servico.descricao && (
                  <p className="text-xs leading-relaxed text-muted-foreground">
                    {servico.descricao}
                  </p>
                )}
                <div className="flex items-center gap-3 text-xs text-muted-foreground">
                  <span className="flex items-center gap-1">
                    <DollarSign className="h-3 w-3" />
                    {formatarPreco(servico.preco)}
                  </span>
                  <span className="flex items-center gap-1">
                    <Clock className="h-3 w-3" />
                    {formatarDuracao(servico.duracao)}
                  </span>
                </div>
              </button>
            );
          })}
        </div>
      )}

      {tentouAvancar && servicosSelecionados.length === 0 && (
        <p className="mt-3 flex items-center gap-1.5 text-xs text-red-500">
          <AlertCircle className="h-3.5 w-3.5" />
          Selecione ao menos um serviço para continuar.
        </p>
      )}

      {servicosSelecionados.length > 0 && (
        <div className="mt-4 flex items-center gap-4 rounded-xl border border-border bg-card px-4 py-3">
          <div className="flex-1">
            <p className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground">
              {servicosSelecionados.length} SERVIÇO(S) SELECIONADO(S)
            </p>
          </div>
          <div className="flex items-center gap-4 text-sm">
            <span className="flex items-center gap-1.5 text-muted-foreground">
              <Clock className="h-3.5 w-3.5" />
              {formatarDuracao(duracaoTotal)}
            </span>
            <span className="flex items-center gap-1.5 font-semibold text-foreground">
              <DollarSign className="h-3.5 w-3.5" />
              {formatarPreco(valorTotal)}
            </span>
          </div>
        </div>
      )}

      <div className="mt-8 flex items-center justify-between">
        <Button
          type="button"
          variant="outline"
          onClick={onBack}
          className="h-10 rounded-full border-border bg-transparent px-5 text-sm text-muted-foreground hover:bg-accent hover:text-foreground"
        >
          <ChevronLeft className="mr-1 h-4 w-4" />
          Voltar
        </Button>
        <Button
          type="button"
          onClick={handleNext}
          disabled={tentouAvancar && servicosSelecionados.length === 0}
          className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 disabled:opacity-50"
        >
          Próximo
          <ChevronRight className="ml-1 h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}
