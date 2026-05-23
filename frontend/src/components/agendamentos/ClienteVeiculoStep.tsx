import { AlertCircle, Car, ChevronRight, RefreshCw, Search, User, X } from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { agendamentoService } from '@/services/agendamentoService';

import type { ClienteResumido, VeiculoResumido } from '@/types/agendamento';

function formatarDoc(cpf?: string, cnpj?: string): string {
  if (cpf?.length === 11) {
    return `${cpf.slice(0, 3)}.${cpf.slice(3, 6)}.${cpf.slice(6, 9)}-${cpf.slice(9)}`;
  }
  if (cnpj?.length === 14) {
    return `${cnpj.slice(0, 2)}.${cnpj.slice(2, 5)}.${cnpj.slice(5, 8)}/${cnpj.slice(8, 12)}-${cnpj.slice(12)}`;
  }
  return '';
}

function getMinDate(): string {
  const now = new Date();
  const y = now.getFullYear();
  const m = String(now.getMonth() + 1).padStart(2, '0');
  const d = String(now.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

interface ClienteVeiculoStepProps {
  cliente: ClienteResumido | null;
  veiculo: VeiculoResumido | null;
  dataAgendamento: string;
  horaInicio: string;
  onClienteChange: (cliente: ClienteResumido | null) => void;
  onVeiculoChange: (veiculo: VeiculoResumido | null) => void;
  onDataChange: (data: string) => void;
  onHoraChange: (hora: string) => void;
  onNext: () => void;
}

export function ClienteVeiculoStep({
  cliente,
  veiculo,
  dataAgendamento,
  horaInicio,
  onClienteChange,
  onVeiculoChange,
  onDataChange,
  onHoraChange,
  onNext,
}: ClienteVeiculoStepProps) {
  const [busca, setBusca] = useState('');
  const [resultados, setResultados] = useState<ClienteResumido[]>([]);
  const [buscando, setBuscando] = useState(false);
  const [showDropdown, setShowDropdown] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const [veiculos, setVeiculos] = useState<VeiculoResumido[]>([]);
  const [carregandoVeiculos, setCarregandoVeiculos] = useState(false);
  const [erroVeiculos, setErroVeiculos] = useState<string | null>(null);

  const [tentouAvancar, setTentouAvancar] = useState(false);

  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current);

    debounceRef.current = setTimeout(() => {
      setBuscando(true);
      agendamentoService
        .buscarClientes(busca)
        .then((r) => {
          setResultados(r);
        })
        .catch((error) => {
          console.error('Erro ao buscar clientes:', error);
          setResultados([]);
        })
        .finally(() => setBuscando(false));
    }, 300);

    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
  }, [busca]);

  useEffect(() => {
    if (!cliente) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setVeiculos([]);
      return;
    }

    setCarregandoVeiculos(true);
    setErroVeiculos(null);
    agendamentoService
      .buscarVeiculosPorCliente(cliente.id)
      .then((v) => setVeiculos(v))
      .catch(() =>
        setErroVeiculos('Não foi possível carregar os veículos deste cliente. Tente novamente.'),
      )
      .finally(() => setCarregandoVeiculos(false));
  }, [cliente]);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setShowDropdown(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleSelectCliente = useCallback(
    (c: ClienteResumido) => {
      onClienteChange(c);
      onVeiculoChange(null);
      setBusca('');
      setShowDropdown(false);
      setTentouAvancar(false);
    },
    [onClienteChange, onVeiculoChange],
  );

  const handleClearCliente = useCallback(() => {
    onClienteChange(null);
    onVeiculoChange(null);
    setVeiculos([]);
    setBusca('');
  }, [onClienteChange, onVeiculoChange]);

  const handleSelectVeiculo = useCallback(
    (v: VeiculoResumido) => {
      onVeiculoChange(v);
      setTentouAvancar(false);
    },
    [onVeiculoChange],
  );

  const handleRetryVeiculos = useCallback(() => {
    if (!cliente) return;
    setCarregandoVeiculos(true);
    setErroVeiculos(null);
    agendamentoService
      .buscarVeiculosPorCliente(cliente.id)
      .then((v) => setVeiculos(v))
      .catch(() =>
        setErroVeiculos('Não foi possível carregar os veículos deste cliente. Tente novamente.'),
      )
      .finally(() => setCarregandoVeiculos(false));
  }, [cliente]);

  const dataSelecionadaObj = dataAgendamento ? new Date(dataAgendamento + 'T12:00:00') : null;
  const diaDaSemana = dataSelecionadaObj ? dataSelecionadaObj.getDay() : -1;
  const isDomingo = diaDaSemana === 0;

  let erroHora = '';
  if (horaInicio && dataAgendamento && !isDomingo) {
    const [h, m] = horaInicio.split(':').map(Number);
    const horaFloat = Number(h) + Number(m) / 60;

    if (diaDaSemana >= 1 && diaDaSemana <= 5) {
      if (horaFloat < 8 || horaFloat > 18) {
        erroHora = 'Seg a Sex: o horário deve ser entre 08:00 e 18:00.';
      }
    } else if (diaDaSemana === 6) {
      if (horaFloat < 8 || horaFloat > 14) {
        erroHora = 'Sábados: o horário deve ser entre 08:00 e 14:00.';
      }
    }
  }

  const isDataValida = !!dataAgendamento && !isDomingo;
  const isHoraValida = !!horaInicio && !erroHora;
  const isStepValid = !!cliente && !!veiculo && isDataValida && isHoraValida;

  const handleNext = useCallback(() => {
    setTentouAvancar(true);
    if (isStepValid) {
      onNext();
    }
  }, [isStepValid, onNext]);

  return (
    <div>
      <div className="mb-5">
        <h3 className="text-xl font-semibold text-zinc-100">Cliente e Veículo</h3>
        <p className="mt-1 text-sm text-zinc-500">
          Selecione o cliente, veículo e escolha a data e hora do agendamento.
        </p>
      </div>

      <div className="space-y-6">
        <div className="space-y-1.5">
          <Label className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            CLIENTE <span className="text-red-500">*</span>
          </Label>

          {cliente ? (
            <div className="flex items-center gap-3 rounded-xl border border-zinc-700/60 bg-zinc-900/50 px-4 py-3">
              <div className="flex h-9 w-9 items-center justify-center rounded-full bg-red-600/10">
                <User className="h-4 w-4 text-red-500" />
              </div>
              <div className="min-w-0 flex-1">
                <p className="text-sm font-medium text-zinc-100">{cliente.nome}</p>
                <p className="text-xs text-zinc-500">{formatarDoc(cliente.cpf, cliente.cnpj)}</p>
              </div>
              <button
                type="button"
                onClick={handleClearCliente}
                className="shrink-0 rounded-full border border-zinc-700/60 bg-zinc-800/50 p-1.5 text-zinc-400 transition-colors hover:bg-zinc-700/50 hover:text-zinc-200"
                aria-label="Remover cliente"
              >
                <X className="h-3.5 w-3.5" />
              </button>
            </div>
          ) : (
            <div className="relative" ref={dropdownRef}>
              <Search
                className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-zinc-500"
                aria-hidden="true"
              />
              <Input
                type="text"
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
                onFocus={() => setShowDropdown(true)}
                placeholder="Buscar por nome, CPF ou CNPJ…"
                className={`h-10 rounded-xl pl-9 text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                  tentouAvancar && !cliente
                    ? 'border-red-500/60 bg-red-950/20'
                    : 'border-zinc-700/60 bg-zinc-900/50'
                }`}
              />
              {buscando && (
                <div className="absolute right-3 top-1/2 -translate-y-1/2">
                  <RefreshCw className="h-4 w-4 animate-spin text-zinc-500" />
                </div>
              )}
              {showDropdown && resultados.length > 0 && (
                <div className="absolute left-0 right-0 top-full z-20 mt-1 max-h-52 overflow-y-auto rounded-xl border border-zinc-700/60 bg-zinc-900 shadow-xl">
                  {resultados.map((c) => (
                    <button
                      key={c.id}
                      type="button"
                      onClick={() => handleSelectCliente(c)}
                      className="flex w-full items-center gap-3 px-4 py-2.5 text-left transition-colors hover:bg-zinc-800/60"
                    >
                      <div className="flex h-8 w-8 items-center justify-center rounded-full bg-zinc-800">
                        <User className="h-3.5 w-3.5 text-zinc-400" />
                      </div>
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-medium text-zinc-200">{c.nome}</p>
                        <p className="text-xs text-zinc-500">{formatarDoc(c.cpf, c.cnpj)}</p>
                      </div>
                    </button>
                  ))}
                </div>
              )}

              {showDropdown && !buscando && resultados.length === 0 && (
                <div className="absolute left-0 right-0 top-full z-20 mt-1 rounded-xl border border-zinc-700/60 bg-zinc-900 px-4 py-3 text-sm text-zinc-500 shadow-xl">
                  {busca.trim() ? 'Nenhum cliente encontrado.' : 'Nenhum cliente cadastrado.'}
                </div>
              )}
            </div>
          )}

          {tentouAvancar && !cliente && (
            <p className="flex items-center gap-1.5 text-xs text-red-500">
              <AlertCircle className="h-3.5 w-3.5" />
              Selecione um cliente para continuar.
            </p>
          )}
        </div>

        <div className="space-y-1.5">
          <Label className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            VEÍCULO <span className="text-red-500">*</span>
          </Label>

          {!cliente ? (
            <p className="rounded-xl border border-zinc-800/40 bg-zinc-900/20 px-4 py-3 text-sm text-zinc-600">
              Selecione um cliente primeiro para ver os veículos disponíveis.
            </p>
          ) : carregandoVeiculos ? (
            <div className="flex items-center gap-2 rounded-xl border border-zinc-800/40 bg-zinc-900/20 px-4 py-3 text-sm text-zinc-500">
              <RefreshCw className="h-4 w-4 animate-spin" />
              Carregando veículos…
            </div>
          ) : erroVeiculos ? (
            <div className="rounded-xl border border-red-500/30 bg-red-950/20 px-4 py-3">
              <p className="text-sm text-red-400">{erroVeiculos}</p>
              <Button
                type="button"
                variant="outline"
                onClick={handleRetryVeiculos}
                className="mt-2 h-8 rounded-full border-red-500/30 bg-transparent px-4 text-xs text-red-400 hover:bg-red-950/30"
              >
                <RefreshCw className="mr-1 h-3 w-3" /> Tentar novamente
              </Button>
            </div>
          ) : veiculos.length === 0 ? (
            <div className="rounded-xl border border-amber-500/30 bg-amber-950/20 px-4 py-3">
              <p className="text-sm text-amber-400">
                Este cliente não possui veículos vinculados. Cadastre um veículo para prosseguir.
              </p>
            </div>
          ) : (
            <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
              {veiculos.map((v) => {
                const selected = veiculo?.id === v.id;
                return (
                  <button
                    key={v.id}
                    type="button"
                    onClick={() => handleSelectVeiculo(v)}
                    className={`flex items-center gap-3 rounded-xl border px-4 py-3 text-left transition-all ${
                      selected
                        ? 'border-red-500/50 bg-red-950/20 shadow-[0_0_0_1px_rgba(239,68,68,0.3)]'
                        : 'border-zinc-700/60 bg-zinc-900/50 hover:border-zinc-600 hover:bg-zinc-800/40'
                    }`}
                  >
                    <div
                      className={`flex h-9 w-9 items-center justify-center rounded-full ${
                        selected ? 'bg-red-600/20' : 'bg-zinc-800'
                      }`}
                    >
                      <Car className={`h-4 w-4 ${selected ? 'text-red-500' : 'text-zinc-400'}`} />
                    </div>
                    <div className="min-w-0 flex-1">
                      <p
                        className={`text-sm font-medium ${selected ? 'text-zinc-100' : 'text-zinc-300'}`}
                      >
                        {v.modelo}
                      </p>
                      <p className="text-xs text-zinc-500">
                        {v.placa} · {v.cor}
                        {v.ano ? ` · ${v.ano}` : ''}
                      </p>
                    </div>
                    {selected && (
                      <div className="flex h-5 w-5 items-center justify-center rounded-full bg-red-600">
                        <svg
                          className="h-3 w-3 text-white"
                          fill="none"
                          viewBox="0 0 24 24"
                          stroke="currentColor"
                          strokeWidth={3}
                        >
                          <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                        </svg>
                      </div>
                    )}
                  </button>
                );
              })}
            </div>
          )}

          {tentouAvancar && cliente && !veiculo && veiculos.length > 0 && (
            <p className="flex items-center gap-1.5 text-xs text-red-500">
              <AlertCircle className="h-3.5 w-3.5" />
              Selecione um veículo para continuar.
            </p>
          )}
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-1.5">
            <Label
              htmlFor="ag-data"
              className="text-[10px] font-bold tracking-[0.2em] text-zinc-500"
            >
              DATA <span className="text-red-500">*</span>
            </Label>
            <Input
              id="ag-data"
              type="date"
              value={dataAgendamento}
              min={getMinDate()}
              onChange={(e) => onDataChange(e.target.value)}
              onKeyDown={(e) => {
                if (e.key !== 'Tab') e.preventDefault();
              }}
              onClick={(e) => {
                try {
                  e.currentTarget.showPicker();
                } catch {
                  // Ignore
                }
              }}
              className={`h-10 rounded-xl text-sm text-zinc-200 focus-visible:ring-0 [color-scheme:dark] ${
                (tentouAvancar && !dataAgendamento) || isDomingo
                  ? 'border-red-500/60 bg-red-950/20'
                  : 'border-zinc-700/60 bg-zinc-900/50'
              }`}
            />
            {tentouAvancar && !dataAgendamento && (
              <p className="flex items-center gap-1.5 text-xs text-red-500">
                <AlertCircle className="h-3.5 w-3.5" />
                Selecione a data do agendamento.
              </p>
            )}
            {isDomingo && (
              <p className="flex items-center gap-1.5 text-xs text-red-500">
                <AlertCircle className="h-3.5 w-3.5" />
                Não há expediente aos domingos.
              </p>
            )}
          </div>

          <div className="space-y-1.5">
            <Label
              htmlFor="ag-hora"
              className="text-[10px] font-bold tracking-[0.2em] text-zinc-500"
            >
              HORÁRIO DE INÍCIO <span className="text-red-500">*</span>
            </Label>
            <Input
              id="ag-hora"
              type="time"
              value={horaInicio}
              onChange={(e) => onHoraChange(e.target.value)}
              onKeyDown={(e) => {
                if (e.key !== 'Tab') e.preventDefault();
              }}
              onClick={(e) => {
                try {
                  e.currentTarget.showPicker();
                } catch {
                  // Ignore
                }
              }}
              className={`h-10 rounded-xl text-sm text-zinc-200 focus-visible:ring-0 [color-scheme:dark] ${
                (tentouAvancar && !horaInicio) || !!erroHora || isDomingo
                  ? 'border-red-500/60 bg-red-950/20'
                  : 'border-zinc-700/60 bg-zinc-900/50'
              }`}
            />
            {tentouAvancar && !horaInicio && (
              <p className="flex items-center gap-1.5 text-xs text-red-500">
                <AlertCircle className="h-3.5 w-3.5" />
                Selecione o horário de início.
              </p>
            )}
            {!!erroHora && !isDomingo && (
              <p className="flex items-center gap-1.5 text-xs text-red-500">
                <AlertCircle className="h-3.5 w-3.5" />
                {erroHora}
              </p>
            )}
            {isDomingo && (
              <p className="flex items-center gap-1.5 text-xs text-red-500">
                <AlertCircle className="h-3.5 w-3.5" />
                Não há expediente aos domingos.
              </p>
            )}
          </div>
        </div>
      </div>
      <div className="mt-8 flex items-center justify-end">
        <Button
          type="button"
          onClick={handleNext}
          disabled={tentouAvancar && !isStepValid}
          className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 disabled:opacity-50"
        >
          Próximo
          <ChevronRight className="ml-1 h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}
