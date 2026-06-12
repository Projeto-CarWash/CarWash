import { Car, CarFront, ChevronLeft, ChevronRight, Link2, Plus, Search, User } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { getCorCSS } from '@/lib/colors';
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
            <h1 className="text-3xl font-bold tracking-tight text-foreground">Veículos</h1>
            <p className="mt-0.5 text-sm text-muted-foreground">
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
            className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
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
            className="h-10 rounded-xl border-border bg-card pl-9 text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0"
          />
        </div>

        <div
          role="group"
          aria-label="Filtrar por status"
          className="flex items-center gap-1 rounded-full border border-border bg-card p-1"
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
                  : 'text-muted-foreground hover:bg-accent hover:text-foreground'
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

      <div className="overflow-hidden rounded-2xl border border-border bg-card">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-border bg-card text-[10px] font-bold uppercase tracking-[0.2em] text-muted-foreground">
            <tr>
              <th className="px-4 py-3">Veículo</th>
              <th className="px-4 py-3">Cor</th>
              <th className="px-4 py-3">Cliente vinculado</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3 text-right">Ações</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {carregando && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">
                  Carregando…
                </td>
              </tr>
            )}
            {!carregando && itens.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">
                  Nenhum veículo encontrado.
                </td>
              </tr>
            )}
            {!carregando &&
              itens.map((v) => (
                <tr key={v.id} className="text-foreground hover:bg-accent">
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      <span
                        className="flex h-9 w-9 items-center justify-center rounded-lg bg-muted text-muted-foreground"
                        aria-hidden="true"
                      >
                        <Car className="h-4 w-4" />
                      </span>
                      <div className="min-w-0">
                        <div className="flex items-center gap-2">
                          <span className="rounded-md border border-border bg-background px-2 py-0.5 font-mono text-xs font-bold tracking-wider text-foreground">
                            {formatarPlaca(v.placa)}
                          </span>
                        </div>
                        <p className="mt-1 truncate text-xs text-muted-foreground">
                          {v.fabricante} {v.modelo}
                          {v.ano ? ` · ${v.ano}` : ''}
                        </p>
                      </div>
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    {v.cor ? (
                      <span
                        className="inline-block h-4 w-4 rounded-full border border-border shadow-sm"
                        style={{ backgroundColor: getCorCSS(v.cor) }}
                        title={`Cor: ${v.cor}`}
                      />
                    ) : (
                      <span className="text-muted-foreground">—</span>
                    )}
                  </td>
                  <td className="px-4 py-3">
                    <button
                      type="button"
                      onClick={() => void navigate(`/clientes/${v.clienteId}`)}
                      className="group inline-flex items-center gap-2 text-left"
                      title={`Ver cliente ${v.clienteNome}`}
                    >
                      <span className="flex h-6 w-6 items-center justify-center rounded-full bg-muted text-[10px] font-bold text-foreground">
                        {(v.clienteNome[0] ?? '?').toUpperCase()}
                      </span>
                      <span className="text-sm text-foreground group-hover:text-red-400 group-hover:underline">
                        {v.clienteNome}
                      </span>
                      {!v.clienteAtivo && (
                        <span className="rounded-full bg-muted px-1.5 py-0.5 text-[9px] font-bold tracking-[0.15em] text-muted-foreground">
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
                          : 'rounded-full bg-muted px-2 py-1 text-[10px] font-bold tracking-[0.15em] text-muted-foreground'
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
                        className="h-8 w-8 text-muted-foreground hover:bg-accent hover:text-foreground"
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
                        className="h-8 w-8 text-muted-foreground hover:bg-accent hover:text-foreground"
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
        <span className="text-xs text-muted-foreground">
          Página {pagina} de {totalPaginas}
        </span>
        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant="outline"
            disabled={!podeVoltar}
            onClick={() => setPagina((p) => Math.max(1, p - 1))}
            className="h-9 rounded-full border-border bg-transparent px-3 text-sm disabled:opacity-40"
          >
            <ChevronLeft className="h-4 w-4" /> Anterior
          </Button>
          <Button
            type="button"
            variant="outline"
            disabled={!podeAvancar}
            onClick={() => setPagina((p) => p + 1)}
            className="h-9 rounded-full border-border bg-transparent px-3 text-sm disabled:opacity-40"
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
