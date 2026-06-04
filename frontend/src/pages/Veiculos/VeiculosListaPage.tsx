import { Car, CarFront, ChevronLeft, ChevronRight, Link2, Plus, Search, User } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { veiculoService } from '@/services/veiculoService';

import type { VeiculoListaItem } from '@/types/veiculo';

const TAMANHO_PAGINA = 20;

type FiltroStatus = 'todos' | 'ativos' | 'inativos';

const FILTROS: { valor: FiltroStatus; rotulo: string }[] = [
  { valor: 'todos', rotulo: 'Todos' },
  { valor: 'ativos', rotulo: 'Ativos' },
  { valor: 'inativos', rotulo: 'Inativos' },
];

export function VeiculosListaPage() {
  const navigate = useNavigate();
  const [busca, setBusca] = useState('');
  const [filtro, setFiltro] = useState<FiltroStatus>('todos');
  const [pagina, setPagina] = useState(1);
  const [itens, setItens] = useState<VeiculoListaItem[]>([]);
  const [total, setTotal] = useState(0);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);

  useEffect(() => {
    let cancelado = false;
    void (async () => {
      setCarregando(true);
      try {
        const resp = await veiculoService.listar({
          busca: busca || undefined,
          ativo: filtro === 'todos' ? undefined : filtro === 'ativos',
          pagina,
          tamanhoPagina: TAMANHO_PAGINA,
        });
        if (cancelado) return;
        setItens(resp.itens);
        setTotal(resp.total);
        setErro(null);
      } catch {
        if (cancelado) return;
        setErro('Não foi possível carregar a lista de veículos.');
      } finally {
        if (!cancelado) setCarregando(false);
      }
    })();
    return () => {
      cancelado = true;
    };
  }, [busca, filtro, pagina]);

  const totalPaginas = Math.max(1, Math.ceil(total / TAMANHO_PAGINA));
  const podeVoltar = pagina > 1;
  const podeAvancar = pagina < totalPaginas;

  return (
    <div className="px-8 py-8">
      {/* Breadcrumb (espelha os mockups: ADMIN / VEÍCULOS) */}

      <div className="mb-6 flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <span
            className="flex h-11 w-11 items-center justify-center rounded-xl bg-red-600/10 text-red-500"
            aria-hidden="true"
          >
            <CarFront className="h-5 w-5" />
          </span>
          <div>
            <h1 className="text-3xl font-bold tracking-tight text-zinc-50">Veículos</h1>
            <p className="mt-0.5 text-sm text-zinc-500">
              {total === 0 ? 'Nenhum veículo cadastrado' : `${total} veículo(s) no total`}
            </p>
          </div>
        </div>
        <Button
          type="button"
          onClick={() => void navigate('/veiculos/novo')}
          className="h-10 rounded-full bg-red-600 px-5 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700"
        >
          <Plus className="mr-1 h-4 w-4" /> Vincular veículo
        </Button>
      </div>

      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="relative max-w-md flex-1">
          <Search
            className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-zinc-500"
            aria-hidden="true"
          />
          <Input
            type="text"
            value={busca}
            onChange={(e) => {
              setPagina(1);
              setBusca(e.target.value);
            }}
            placeholder="Buscar por placa, modelo, fabricante ou cliente…"
            aria-label="Buscar veículos"
            className="h-10 rounded-xl border-zinc-700/60 bg-zinc-900/50 pl-9 text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0"
          />
        </div>

        <div
          role="group"
          aria-label="Filtrar por status"
          className="flex items-center gap-1 rounded-full border border-zinc-800/60 bg-zinc-900/40 p-1"
        >
          {FILTROS.map((f) => (
            <button
              key={f.valor}
              type="button"
              onClick={() => {
                setPagina(1);
                setFiltro(f.valor);
              }}
              aria-pressed={filtro === f.valor}
              className={`rounded-full px-3 py-1.5 text-xs font-semibold transition-colors ${
                filtro === f.valor
                  ? 'bg-red-600 text-white'
                  : 'text-zinc-400 hover:bg-zinc-800/60 hover:text-zinc-200'
              }`}
            >
              {f.rotulo}
            </button>
          ))}
        </div>
      </div>

      {erro && (
        <div
          role="alert"
          className="mb-4 rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3 text-sm text-red-400"
        >
          {erro}
        </div>
      )}

      <div className="overflow-hidden rounded-2xl border border-zinc-800/60 bg-zinc-900/30">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-zinc-800/60 bg-zinc-900/60 text-[10px] font-bold uppercase tracking-[0.2em] text-zinc-500">
            <tr>
              <th className="px-4 py-3">Veículo</th>
              <th className="px-4 py-3">Cor</th>
              <th className="px-4 py-3">Cliente vinculado</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3 text-right">Ações</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-zinc-800/60">
            {carregando && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-zinc-500">
                  Carregando…
                </td>
              </tr>
            )}
            {!carregando && itens.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-zinc-500">
                  Nenhum veículo encontrado.
                </td>
              </tr>
            )}
            {!carregando &&
              itens.map((v) => (
                <tr key={v.id} className="text-zinc-200 hover:bg-zinc-800/30">
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      <span
                        className="flex h-9 w-9 items-center justify-center rounded-lg bg-zinc-800/60 text-zinc-400"
                        aria-hidden="true"
                      >
                        <Car className="h-4 w-4" />
                      </span>
                      <div className="min-w-0">
                        <div className="flex items-center gap-2">
                          <span className="rounded-md border border-zinc-700/60 bg-zinc-950/60 px-2 py-0.5 font-mono text-xs font-bold tracking-wider text-zinc-100">
                            {formatarPlaca(v.placa)}
                          </span>
                        </div>
                        <p className="mt-1 truncate text-xs text-zinc-400">
                          {v.fabricante} {v.modelo}
                          {v.ano ? ` · ${v.ano}` : ''}
                        </p>
                      </div>
                    </div>
                  </td>
                  <td className="px-4 py-3 text-zinc-400">{v.cor}</td>
                  <td className="px-4 py-3">
                    <button
                      type="button"
                      onClick={() => void navigate(`/clientes/${v.clienteId}`)}
                      className="group inline-flex items-center gap-2 text-left"
                      title={`Ver cliente ${v.clienteNome}`}
                    >
                      <span className="flex h-6 w-6 items-center justify-center rounded-full bg-zinc-800 text-[10px] font-bold text-zinc-300">
                        {(v.clienteNome[0] ?? '?').toUpperCase()}
                      </span>
                      <span className="text-sm text-zinc-200 group-hover:text-red-400 group-hover:underline">
                        {v.clienteNome}
                      </span>
                      {!v.clienteAtivo && (
                        <span className="rounded-full bg-zinc-700/40 px-1.5 py-0.5 text-[9px] font-bold tracking-[0.15em] text-zinc-500">
                          INATIVO
                        </span>
                      )}
                    </button>
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={
                        v.ativo
                          ? 'rounded-full bg-green-500/10 px-2 py-1 text-[10px] font-bold tracking-[0.15em] text-green-400'
                          : 'rounded-full bg-zinc-700/40 px-2 py-1 text-[10px] font-bold tracking-[0.15em] text-zinc-500'
                      }
                    >
                      {v.ativo ? 'ATIVO' : 'INATIVO'}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex justify-end gap-2">
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() => void navigate(`/clientes/${v.clienteId}`)}
                        title="Visualizar cliente vinculado"
                        className="h-8 w-8 text-zinc-400 hover:bg-zinc-800 hover:text-zinc-100"
                      >
                        <User className="h-4 w-4" />
                        <span className="sr-only">Ver cliente de {formatarPlaca(v.placa)}</span>
                      </Button>
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() => void navigate(`/clientes/${v.clienteId}/veiculos/novo`)}
                        title="Vincular outro veículo a este cliente"
                        className="h-8 w-8 text-zinc-400 hover:bg-zinc-800 hover:text-zinc-100"
                      >
                        <Link2 className="h-4 w-4" />
                        <span className="sr-only">Vincular outro veículo a {v.clienteNome}</span>
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      <div className="mt-4 flex items-center justify-between">
        <span className="text-xs text-zinc-500">
          Página {pagina} de {totalPaginas}
        </span>
        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant="outline"
            disabled={!podeVoltar}
            onClick={() => setPagina((p) => Math.max(1, p - 1))}
            className="h-9 rounded-full border-zinc-700/60 bg-transparent px-3 text-sm disabled:opacity-40"
          >
            <ChevronLeft className="h-4 w-4" /> Anterior
          </Button>
          <Button
            type="button"
            variant="outline"
            disabled={!podeAvancar}
            onClick={() => setPagina((p) => p + 1)}
            className="h-9 rounded-full border-zinc-700/60 bg-transparent px-3 text-sm disabled:opacity-40"
          >
            Próxima <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}

/** Formata a placa normalizada (sem separador) com hífen após os 3 primeiros caracteres. */
function formatarPlaca(placa: string): string {
  if (placa.length !== 7) return placa;
  return `${placa.slice(0, 3)}-${placa.slice(3)}`;
}
