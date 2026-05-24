import { AlertTriangle, ArrowLeft, Car, Plus, Power, RefreshCcw } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { AxiosError } from 'axios';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { clienteService, type ClienteDetalhe } from '@/services/clienteService';

export function ClienteDetalhePage() {
  const { id = '' } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [cliente, setCliente] = useState<ClienteDetalhe | null>(null);
  const [erro, setErro] = useState<string | null>(null);
  const [salvando, setSalvando] = useState(false);
  const [carregando, setCarregando] = useState(true);
  const [erroHttp, setErroHttp] = useState<number | null>(null);

  const carregarCliente = useCallback(async () => {
    setCarregando(true);
    setErro(null);
    setErroHttp(null);
    try {
      const c = await clienteService.obterPorId(id);
      setCliente(c);
    } catch (error) {
      if (error instanceof AxiosError && error.response) {
        const status = error.response.status;
        setErroHttp(status);
        if (status === 401) {
          navigate('/login', { replace: true });
        } else if (status === 403) {
          setErro('Você não possui permissão para consultar clientes.');
        } else if (status === 404) {
          setErro('Cliente não encontrado.');
        } else {
          setErro('Não foi possível concluir a consulta no momento. Tente novamente.');
        }
      } else {
        setErro('Não foi possível concluir a consulta no momento. Tente novamente.');
        setErroHttp(500);
      }
    } finally {
      setCarregando(false);
    }
  }, [id, navigate]);

  useEffect(() => {
    setCliente(null);
    void carregarCliente();
  }, [carregarCliente]);

  const toggleStatus = useCallback(async () => {
    if (!cliente) return;
    setSalvando(true);
    setErro(null);
    try {
      const novo = await clienteService.alterarStatus(cliente.id, !cliente.ativo);
      setCliente((prev) => (prev ? { ...prev, ativo: novo.ativo } : null));
    } catch {
      setErro('Não foi possível alterar o status do cliente.');
    } finally {
      setSalvando(false);
    }
  }, [cliente]);

  if (erro && !cliente && erroHttp === 404) {
    return (
      <div className="px-8 py-8">
        <Button
          type="button"
          variant="outline"
          onClick={() => void navigate('/clientes')}
          className="mb-4 h-9 rounded-full border-zinc-700/60 bg-transparent px-4 text-sm"
        >
          <ArrowLeft className="mr-1 h-4 w-4" /> Voltar
        </Button>
        <p role="alert" className="text-sm text-red-400">
          {erro}
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-6 px-8 py-8">
      <div className="flex items-center justify-between">
        <Button
          type="button"
          variant="outline"
          onClick={() => void navigate('/clientes')}
          className="h-9 rounded-full border-zinc-700/60 bg-transparent px-4 text-sm hover:bg-zinc-800/50 hover:text-zinc-200 text-zinc-400"
        >
          <ArrowLeft className="mr-1 h-4 w-4" /> Voltar.
        </Button>
        <Button
          type="button"
          variant="outline"
          disabled={salvando || carregando || !cliente}
          onClick={toggleStatus}
          className="h-9 rounded-full border-zinc-700/60 bg-transparent px-4 text-sm hover:bg-zinc-800/50 hover:text-zinc-200 text-zinc-400"
        >
          <Power className="mr-1 h-4 w-4" />
          {cliente?.ativo ? 'Inativar cliente' : 'Reativar cliente'}
        </Button>
      </div>

      <Card className="border-zinc-800/60 bg-zinc-900/30">
        <CardHeader>
          <CardTitle className="flex items-center gap-3 text-xl text-zinc-100">
            {carregando && !cliente ? 'Carregando...' : cliente?.nome}
            {cliente && (
              <span
                className={
                  cliente.ativo
                    ? 'rounded-full bg-green-500/10 px-2 py-0.5 text-[10px] font-bold tracking-[0.15em] text-green-400'
                    : 'rounded-full bg-zinc-700/40 px-2 py-0.5 text-[10px] font-bold tracking-[0.15em] text-zinc-500'
                }
              >
                {cliente.ativo ? 'ATIVO' : 'INATIVO'}
              </span>
            )}
          </CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-4 text-sm text-zinc-300 sm:grid-cols-3">
          {carregando && !cliente ? (
            <div className="col-span-3 h-32 animate-pulse bg-zinc-800/30 rounded-lg"></div>
          ) : cliente ? (
            <>
              <Campo label="DOCUMENTO" valor={cliente.cpf ?? cliente.cnpj ?? '—'} />
              <Campo label="NASCIMENTO" valor={cliente.dataNascimento} />
              <Campo label="CELULAR" valor={cliente.celular} />
              {cliente.telefone && <Campo label="TELEFONE" valor={cliente.telefone} />}
              {cliente.email && <Campo label="E-MAIL" valor={cliente.email} />}
              <Campo
                label="ENDEREÇO"
                valor={`${cliente.endereco.logradouro}, ${cliente.endereco.numero}${cliente.endereco.complemento ? ' — ' + cliente.endereco.complemento : ''}`}
              />
              <Campo
                label="BAIRRO / CIDADE / UF"
                valor={`${cliente.endereco.bairro} — ${cliente.endereco.cidade} / ${cliente.endereco.uf}`}
              />
              <Campo label="CEP" valor={cliente.endereco.cep} />
              <Campo label="CRIADO EM" valor={new Date(cliente.criadoEm).toLocaleString('pt-BR')} />
              <Campo
                label="ATUALIZADO EM"
                valor={new Date(cliente.atualizadoEm).toLocaleString('pt-BR')}
              />
            </>
          ) : null}
        </CardContent>
      </Card>

      <Card className="border-zinc-800/60 bg-zinc-900/30">
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-4">
          <div>
            <h2 className="text-lg text-zinc-100 flex items-center gap-2 font-semibold">
              <Car className="h-5 w-5 text-red-500" />
              Veículos do cliente <span className="text-sm font-normal text-zinc-400">({cliente?.veiculos?.length ?? 0})</span>
            </h2>
            <p className="text-xs text-zinc-400 mt-1">
              Veículos associados a este cliente para ordens de serviço.
            </p>
          </div>
          {!carregando && !erro && (
            <Button
              type="button"
              onClick={() => void navigate(`/clientes/${id}/veiculos/novo`)}
              className="h-8 rounded-full bg-red-600 px-3 text-xs font-semibold text-white hover:bg-red-700 shadow-lg shadow-red-600/15"
            >
              <Plus className="mr-1 h-3.5 w-3.5" /> Adicionar veículo
            </Button>
          )}
        </CardHeader>
        <CardContent>
          {carregando ? (
            <ul aria-busy="true" className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {[1, 2, 3].map((skeleton) => (
                <li key={skeleton} className="flex h-20 items-center justify-between rounded-xl border border-zinc-800/40 bg-zinc-900/20 p-4 animate-pulse">
                  <div className="flex items-center gap-3 w-full">
                    <div className="h-9 w-9 rounded-lg bg-zinc-800/50"></div>
                    <div className="space-y-2 w-full">
                      <div className="h-4 w-1/2 rounded bg-zinc-800/50"></div>
                      <div className="h-3 w-1/3 rounded bg-zinc-800/50"></div>
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          ) : erro ? (
            <div className="flex flex-col items-center justify-center rounded-xl border border-dashed border-red-500/20 bg-red-950/10 py-8 text-center" aria-live="assertive">
              <AlertTriangle className="h-8 w-8 text-red-500/80 mb-3" />
              <p className="text-sm font-medium text-red-400">{erro}</p>
              <Button
                type="button"
                onClick={carregarCliente}
                className="mt-4 h-9 rounded-full bg-red-600/10 hover:bg-red-600/20 text-xs font-semibold text-red-400 px-4 border border-red-500/30"
              >
                <RefreshCcw className="mr-1 h-3.5 w-3.5" /> Tentar novamente
              </Button>
            </div>
          ) : (cliente?.veiculos || []).length === 0 ? (
            <div className="flex flex-col items-center justify-center rounded-xl border border-dashed border-zinc-800 bg-zinc-950/20 py-8 text-center">
              <div className="flex h-12 w-12 items-center justify-center rounded-full bg-zinc-800/40 text-zinc-500 mb-3">
                <Car className="h-6 w-6" />
              </div>
              <h4 className="text-sm font-semibold text-zinc-200">Nenhum veículo vinculado</h4>
              <p className="mt-1 text-xs text-zinc-500 max-w-[280px]">
                Este cliente ainda não possui veículos cadastrados.
              </p>
              <Button
                type="button"
                onClick={() => void navigate(`/clientes/${id}/veiculos/novo`)}
                className="mt-4 h-9 rounded-full bg-zinc-800 hover:bg-zinc-700 text-xs font-semibold text-zinc-200 px-4 border border-zinc-700/60"
              >
                <Plus className="mr-1 h-3.5 w-3.5" /> Adicionar veículo
              </Button>
            </div>
          ) : (
            <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {[...(cliente?.veiculos || [])]
                .reverse()
                .map((veiculo) => (
                <li
                  key={veiculo.id || veiculo.placa}
                  className="flex items-center justify-between rounded-xl border border-zinc-800 bg-zinc-950/30 p-4 hover:border-zinc-700 transition-colors"
                >
                  <div className="flex items-center gap-3">
                    <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-zinc-800/60 text-zinc-400">
                      <Car className="h-5 w-5" />
                    </div>
                    <div>
                      <h4 className="text-sm font-semibold text-zinc-200">
                        {veiculo.fabricante} {veiculo.modelo}
                      </h4>
                      <p className="text-xs text-zinc-500">
                        {veiculo.cor}
                      </p>
                    </div>
                  </div>
                  <div className="rounded-md border border-zinc-700 bg-zinc-900 px-2.5 py-1 text-xs font-mono font-bold tracking-wider text-zinc-200 uppercase shadow-inner">
                    {formatarPlacaExibicao(veiculo.placa)}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function formatarPlacaExibicao(placa: string) {
  const clean = placa.toUpperCase().replace(/\s+/g, '').replace(/-/g, '');
  if (clean.length === 7) {
    const fifthChar = clean.charAt(4);
    const isDigit = fifthChar >= '0' && fifthChar <= '9';
    if (isDigit) {
      return `${clean.slice(0, 3)}-${clean.slice(3)}`;
    }
  }
  return clean;
}

function Campo({ label, valor }: { label: string; valor: string }) {
  return (
    <div>
      <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-zinc-500">{label}</p>
      <p className="mt-1 text-zinc-200">{valor}</p>
    </div>
  );
}
