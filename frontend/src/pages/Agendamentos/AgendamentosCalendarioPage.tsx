import { format, startOfWeek, endOfWeek, addWeeks, subWeeks, addDays, isSameDay } from 'date-fns';
import { ptBR } from 'date-fns/locale/pt-BR';
import {
  ArrowLeft,
  ChevronLeft,
  ChevronRight,
  Loader2,
  CheckCircle2,
  Clock,
  XCircle,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';

import { agendamentoService } from '@/services/agendamentoService';

import type { AgendamentoSemana } from '@/types/agendamento';

export function AgendamentosCalendarioPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const paramAno = searchParams.get('ano');
  const paramMes = searchParams.get('mes'); // 1 a 12

  const [currentDate, setCurrentDate] = useState(() => {
    if (paramAno && paramMes) {
      const targetAno = Number(paramAno);
      const targetMes = Number(paramMes) - 1;
      const today = new Date();

      if (today.getFullYear() === targetAno && today.getMonth() === targetMes) {
        return today;
      }

      return new Date(targetAno, targetMes, 1);
    }
    return new Date();
  });

  const [agendamentos, setAgendamentos] = useState<AgendamentoSemana[]>([]);
  const [loading, setLoading] = useState(true);

  const startDate = startOfWeek(currentDate, { weekStartsOn: 1 });
  const endDate = endOfWeek(currentDate, { weekStartsOn: 1 });

  useEffect(() => {
    const fetchAgendamentos = async () => {
      setLoading(true);
      try {
        const dados = await agendamentoService.listarAgendamentosSemana(startDate, endDate);
        setAgendamentos(dados);
      } catch (error) {
        console.error('Erro ao buscar agendamentos:', error);
      } finally {
        setLoading(false);
      }
    };

    void fetchAgendamentos();
  }, [startDate.getTime(), endDate.getTime()]);

  const handlePrevWeek = () => setCurrentDate((prev) => subWeeks(prev, 1));
  const handleNextWeek = () => setCurrentDate((prev) => addWeeks(prev, 1));
  const handleCurrentWeek = () => setCurrentDate(new Date());

  const weekDays = Array.from({ length: 7 }).map((_, i) => addDays(startDate, i));

  const getAgendamentosPorDia = (date: Date) => {
    return agendamentos
      .filter((ag) => {
        const agDate = new Date(ag.inicio);
        return isSameDay(agDate, date);
      })
      .sort((a, b) => new Date(a.inicio).getTime() - new Date(b.inicio).getTime());
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'agendado':
        return <CheckCircle2 className="h-3 w-3 text-emerald-500" />;
      case 'pendente':
        return <Clock className="h-3 w-3 text-amber-500" />;
      case 'cancelado':
        return <XCircle className="h-3 w-3 text-red-600" />;
      default:
        return null;
    }
  };

  const getStatusBorder = (status: string) => {
    switch (status) {
      case 'agendado':
        return 'border-emerald-500/30 hover:border-emerald-500/60';
      case 'pendente':
        return 'border-amber-500/30 hover:border-amber-500/60';
      case 'cancelado':
        return 'border-red-600/30 hover:border-red-600/60';
      default:
        return 'border-zinc-800/60';
    }
  };

  return (
    <div className="flex h-full flex-col px-6 lg:px-8 py-6 text-white">
      <div className="flex items-center justify-between pb-8">
        <div className="flex items-center gap-3">
          <h1 className="text-2xl font-bold tracking-tight">Agendamentos</h1>
          <div className="flex items-center gap-2">
            <div className="flex h-6 items-center rounded-md border border-zinc-800 bg-zinc-900/50 px-2.5 text-[10px] font-semibold tracking-wider text-zinc-400 uppercase">
              {format(startDate, 'yyyy - MMMM', { locale: ptBR })}
            </div>
            {startDate.getMonth() === new Date().getMonth() &&
              startDate.getFullYear() === new Date().getFullYear() && (
                <div className="flex h-6 items-center rounded-md border border-red-500/30 bg-red-500/10 px-2.5 text-[10px] font-semibold tracking-wider text-red-500 uppercase">
                  Mês Atual
                </div>
              )}
          </div>
        </div>
      </div>

      <div className="flex items-center justify-between pb-6">
        <button
          onClick={() => navigate('/agendamentos')}
          className="flex items-center gap-2 rounded-lg border border-zinc-800 bg-zinc-900/50 px-4 py-2 text-xs font-semibold text-zinc-300 transition-colors hover:bg-zinc-800 hover:text-white"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          Voltar aos Meses
        </button>

        <div className="flex items-center gap-4">
          <div className="flex items-center rounded-lg border border-zinc-800 bg-zinc-900/50 p-1">
            <button
              onClick={handlePrevWeek}
              className="flex items-center gap-1.5 rounded-md px-3 py-1.5 text-xs font-medium text-zinc-400 hover:bg-zinc-800 hover:text-white"
            >
              <ChevronLeft className="h-3.5 w-3.5" />
              Semana Anterior
            </button>
            <div className="mx-1 h-4 w-px bg-zinc-800" />
            <button
              onClick={handleCurrentWeek}
              className="rounded-md px-3 py-1.5 text-xs font-medium text-red-500 hover:bg-red-500/10 border border-transparent hover:border-red-500/30"
              style={
                isSameDay(new Date(), currentDate)
                  ? {
                      borderColor: 'rgba(239, 68, 68, 0.3)',
                      backgroundColor: 'rgba(239, 68, 68, 0.05)',
                    }
                  : {}
              }
            >
              Semana Atual
            </button>
            <div className="mx-1 h-4 w-px bg-zinc-800" />
            <button
              onClick={handleNextWeek}
              className="flex items-center gap-1.5 rounded-md px-3 py-1.5 text-xs font-medium text-zinc-400 hover:bg-zinc-800 hover:text-white"
            >
              Próxima Semana
              <ChevronRight className="h-3.5 w-3.5" />
            </button>
          </div>

          <div className="text-xs font-medium text-zinc-400 tracking-wider">
            {format(startDate, "dd 'de' MMM.", { locale: ptBR })} -{' '}
            {format(endDate, "dd 'de' MMM.", { locale: ptBR })}
          </div>
        </div>
      </div>

      <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-xl border border-zinc-800/60 bg-[#0c0c0e]">
        <div className="grid grid-cols-7 border-b border-zinc-800/60 bg-zinc-900/50">
          {weekDays.map((day) => {
            const isToday = isSameDay(day, new Date());
            return (
              <div
                key={day.toISOString()}
                className={`flex flex-col items-center justify-center py-4 border-r border-zinc-800/60 last:border-r-0 ${isToday ? 'bg-zinc-800/30' : ''}`}
              >
                <span
                  className={`text-[10px] font-bold tracking-[0.15em] uppercase ${isToday ? 'text-red-500' : 'text-zinc-500'}`}
                >
                  {format(day, 'E', { locale: ptBR }).substring(0, 3)}
                </span>
                <span
                  className={`mt-1 text-lg font-bold ${isToday ? 'text-red-500' : 'text-zinc-200'}`}
                >
                  {format(day, 'd')}
                </span>
              </div>
            );
          })}
        </div>

        <div className="grid grid-cols-7 flex-1 min-h-0 overflow-y-auto">
          {loading ? (
            <div className="col-span-7 flex items-center justify-center h-40">
              <Loader2 className="h-6 w-6 animate-spin text-red-600" />
            </div>
          ) : (
            weekDays.map((day) => {
              const dayAgendamentos = getAgendamentosPorDia(day);
              const isToday = isSameDay(day, new Date());

              return (
                <div
                  key={day.toISOString()}
                  className={`border-r border-zinc-800/60 last:border-r-0 p-3 flex flex-col gap-3 min-h-[400px] ${isToday ? 'bg-red-500/5' : ''}`}
                >
                  {dayAgendamentos.length === 0 ? (
                    <div className="flex h-full items-center justify-center">
                      <span className="text-[10px] font-medium text-zinc-600 uppercase tracking-wider">
                        Sem agendamentos
                      </span>
                    </div>
                  ) : (
                    dayAgendamentos.map((ag) => (
                      <div
                        key={ag.id}
                        className={`group relative flex flex-col gap-1.5 rounded-lg border bg-[#121214] p-3 shadow-sm transition-all cursor-pointer ${getStatusBorder(ag.status)}`}
                      >
                        <div className="absolute inset-0 border-t border-zinc-800/60 transition-colors group-hover:bg-zinc-800/20" />
                        <div className="pointer-events-none absolute inset-x-0 -top-px h-px bg-red-500/50 opacity-0 transition-opacity group-hover:opacity-100" />
                        <div className="relative">
                          <h3
                            className="text-xs font-bold text-zinc-200 truncate"
                            title={ag.titulo}
                          >
                            {ag.titulo}
                          </h3>
                          <div className="text-[10px] text-zinc-400">
                            {format(new Date(ag.inicio), 'HH:mm')} -{' '}
                            {format(new Date(ag.fim), 'HH:mm')}
                          </div>
                          <div className="text-[10px] text-zinc-500 truncate">{ag.cliente}</div>
                          <div className="mt-1">{getStatusIcon(ag.status)}</div>
                        </div>
                      </div>
                    ))
                  )}

                  <div className="mt-auto pt-4">
                    <button
                      onClick={() => navigate('/agendamentos/novo')}
                      className="w-full rounded border border-dashed border-zinc-700 py-2 text-[10px] font-bold text-zinc-500 transition-colors hover:border-zinc-500 hover:text-zinc-300 uppercase tracking-wider"
                    >
                      + Novo
                    </button>
                  </div>
                </div>
              );
            })
          )}
        </div>
      </div>
    </div>
  );
}
