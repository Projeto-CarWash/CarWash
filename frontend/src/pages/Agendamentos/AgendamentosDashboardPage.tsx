import { Loader2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { agendamentoService } from '@/services/agendamentoService';

import type { EstatisticasMes } from '@/types/agendamento';

export function AgendamentosDashboardPage() {
  const navigate = useNavigate();
  const [meses, setMeses] = useState<EstatisticasMes[]>([]);
  const [loading, setLoading] = useState(true);
  const anoAtual = new Date().getFullYear();

  useEffect(() => {
    const carregarEstatisticas = async () => {
      try {
        setLoading(true);
        const dados = await agendamentoService.obterEstatisticasAno(anoAtual);
        setMeses(dados);
      } catch (error) {
        console.error('Erro ao carregar estatísticas:', error);
      } finally {
        setLoading(false);
      }
    };

    void carregarEstatisticas();
  }, [anoAtual]);

  const handleMesClick = (mes: number) => {
    void navigate(`/agendamentos/calendario?ano=${anoAtual}&mes=${mes}`);
  };

  return (
    <div className="flex h-full flex-col px-6 lg:px-8 py-6">
      <div className="flex items-center justify-between pb-8">
        <div className="flex items-center gap-3">
          <h1 className="text-2xl font-bold tracking-tight text-white">Agendamentos</h1>
          <div className="flex h-6 items-center rounded-md border border-zinc-800 bg-zinc-900/50 px-2.5 text-[10px] font-semibold tracking-wider text-zinc-400">
            {anoAtual}
          </div>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto pb-8">
        {loading ? (
          <div className="flex h-full items-center justify-center">
            <Loader2 className="h-8 w-8 animate-spin text-red-600" />
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
            {meses.map((mes) => (
              <button
                key={mes.mes}
                type="button"
                onClick={() => handleMesClick(mes.mes)}
                className="group flex w-full cursor-pointer flex-col rounded-xl border border-zinc-800/60 bg-[#0c0c0e] p-6 text-left transition-all hover:border-red-600/30 hover:bg-zinc-900/40 hover:shadow-[0_0_20px_rgba(220,38,38,0.05)]"
              >
                <div className="mb-6 flex w-full items-center justify-between">
                  <div className="flex items-center gap-3">
                    <h2 className="text-[11px] font-black tracking-[0.15em] text-zinc-100 uppercase transition-colors group-hover:text-white">
                      {mes.nome}
                    </h2>
                    {mes.mes === new Date().getMonth() + 1 &&
                      anoAtual === new Date().getFullYear() && (
                        <div className="flex h-5 items-center rounded border border-red-500/30 bg-red-500/10 px-2 text-[9px] font-bold tracking-wider text-red-500 uppercase">
                          Mês Atual
                        </div>
                      )}
                  </div>
                  <div className="h-1.5 w-1.5 rounded-full bg-zinc-800 transition-colors group-hover:bg-red-600" />
                </div>
                <div className="grid w-full flex-1 grid-cols-2 rounded-lg border border-zinc-800/60 bg-[#08080a] overflow-hidden transition-colors group-hover:border-zinc-700/50">
                  <div className="flex flex-col justify-center border-b border-r border-zinc-800/60 p-6 text-left">
                    <p className="text-[9px] font-bold tracking-[0.15em] text-zinc-600">
                      CONFIRMADOS
                    </p>
                    <p className="mt-3 text-sm font-bold text-emerald-500">{mes.confirmados}</p>
                  </div>
                  <div className="flex flex-col justify-center border-b border-zinc-800/60 p-6 text-left">
                    <p className="text-[9px] font-bold tracking-[0.15em] text-zinc-600">
                      PENDENTES
                    </p>
                    <p className="mt-3 text-sm font-bold text-amber-500">{mes.pendentes}</p>
                  </div>
                  <div className="flex flex-col justify-center border-r border-zinc-800/60 p-6 text-left">
                    <p className="text-[9px] font-bold tracking-[0.15em] text-zinc-600">
                      CANCELADOS
                    </p>
                    <p className="mt-3 text-sm font-bold text-red-600">{mes.cancelados}</p>
                  </div>
                  <div className="flex flex-col justify-center p-6 text-left">
                    <p className="text-[9px] font-bold tracking-[0.15em] text-zinc-600">TOTAL</p>
                    <p className="mt-3 text-sm font-bold text-zinc-200">{mes.total}</p>
                  </div>
                </div>
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
