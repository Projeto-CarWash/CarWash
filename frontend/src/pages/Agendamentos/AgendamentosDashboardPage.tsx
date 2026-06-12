import { Loader2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { agendamentoService } from '@/services/agendamentoService';
import { filialService } from '@/services/filialService';

import type { EstatisticasMes } from '@/types/agendamento';
import type { FilialResumo } from '@/types/filial';

export function AgendamentosDashboardPage() {
  const navigate = useNavigate();
  const [meses, setMeses] = useState<EstatisticasMes[]>([]);
  const [loading, setLoading] = useState(true);
  const [filiais, setFiliais] = useState<FilialResumo[]>([]);
  const [filialId, setFilialId] = useState('');
  const anoAtual = new Date().getFullYear();

  // Carrega as filiais e define a inicial (a primeira da lista).
  useEffect(() => {
    let ignore = false;
    void filialService
      .listar()
      .then((lista) => {
        if (ignore) return;
        setFiliais(lista.itens);
        setFilialId((atual) => (atual ? atual : (lista.itens[0]?.id ?? '')));
      })
      .catch(() => {
        /* sem filiais: estatísticas ficam zeradas */
      });
    return () => {
      ignore = true;
    };
  }, []);

  useEffect(() => {
    if (!filialId) return;
    let ignore = false;
    const carregarEstatisticas = async () => {
      try {
        setLoading(true);
        const dados = await agendamentoService.obterEstatisticasAno(anoAtual, filialId);
        if (!ignore) setMeses(dados);
      } catch (error) {
        console.error('Erro ao carregar estatísticas:', error);
      } finally {
        if (!ignore) setLoading(false);
      }
    };

    void carregarEstatisticas();
    return () => {
      ignore = true;
    };
  }, [anoAtual, filialId]);

  const handleMesClick = (mes: number) => {
    void navigate(`/agendamentos/calendario?ano=${anoAtual}&mes=${mes}&filialId=${filialId}`);
  };

  return (
    <div className="flex h-full flex-col px-6 lg:px-8 py-6">
      <div className="flex items-center justify-between pb-8">
        <div className="flex items-center gap-3">
          <h1 className="text-2xl font-bold tracking-tight text-foreground">Agendamentos</h1>
          <div className="flex h-6 items-center rounded-md border border-border bg-muted px-2.5 text-[10px] font-semibold tracking-wider text-muted-foreground">
            {anoAtual}
          </div>
        </div>

        <div className="flex items-center gap-2">
          <label
            htmlFor="filial-dashboard"
            className="text-[10px] font-bold tracking-wider text-muted-foreground uppercase"
          >
            Filial
          </label>
          <select
            id="filial-dashboard"
            value={filialId}
            onChange={(e) => setFilialId(e.target.value)}
            className="h-9 rounded-lg border border-border bg-muted px-3 text-sm text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500/50"
          >
            {filiais.length === 0 && <option value="">Carregando…</option>}
            {filiais.map((f) => (
              <option key={f.id} value={f.id}>
                {f.nome}
              </option>
            ))}
          </select>
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
                className="group flex w-full cursor-pointer flex-col rounded-xl border border-border bg-card p-6 text-left transition-all hover:border-red-600/30 hover:bg-accent/50 hover:shadow-[0_0_20px_rgba(220,38,38,0.05)]"
              >
                <div className="mb-6 flex w-full items-center justify-between">
                  <div className="flex items-center gap-3">
                    <h2 className="text-[11px] font-black tracking-[0.15em] text-foreground uppercase transition-colors group-hover:text-foreground">
                      {mes.nome}
                    </h2>
                    {mes.mes === new Date().getMonth() + 1 &&
                      anoAtual === new Date().getFullYear() && (
                        <div className="flex h-5 items-center rounded border border-red-500/30 bg-red-500/10 px-2 text-[9px] font-bold tracking-wider text-red-500 uppercase">
                          Mês Atual
                        </div>
                      )}
                  </div>
                  <div className="flex items-center gap-1.5 rounded-md border border-border bg-muted px-2.5 py-1 text-[9px] font-bold tracking-wider text-muted-foreground uppercase">
                    Total
                    <span className="text-foreground">{mes.total}</span>
                  </div>
                </div>
                <div className="grid w-full flex-1 grid-cols-2 rounded-lg border border-border bg-muted overflow-hidden transition-colors group-hover:border-border">
                  <div className="flex flex-col justify-center border-b border-r border-border p-6 text-left">
                    <p className="text-[9px] font-bold tracking-[0.15em] text-muted-foreground">
                      AGENDADO
                    </p>
                    <p className="mt-3 text-sm font-bold text-emerald-500">{mes.agendado}</p>
                  </div>
                  <div className="flex flex-col justify-center border-b border-border p-6 text-left">
                    <p className="text-[9px] font-bold tracking-[0.15em] text-muted-foreground">
                      EM ANDAMENTO
                    </p>
                    <p className="mt-3 text-sm font-bold text-amber-500">{mes.emAndamento}</p>
                  </div>
                  <div className="flex flex-col justify-center border-r border-border p-6 text-left">
                    <p className="text-[9px] font-bold tracking-[0.15em] text-muted-foreground">
                      CONCLUÍDO
                    </p>
                    <p className="mt-3 text-sm font-bold text-blue-500">{mes.concluido}</p>
                  </div>
                  <div className="flex flex-col justify-center p-6 text-left">
                    <p className="text-[9px] font-bold tracking-[0.15em] text-muted-foreground">
                      CANCELADO
                    </p>
                    <p className="mt-3 text-sm font-bold text-red-600">{mes.cancelado}</p>
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
