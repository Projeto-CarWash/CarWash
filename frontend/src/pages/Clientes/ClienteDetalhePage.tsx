import { ArrowLeft, Power } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { clienteService, type ClienteDetalhe } from '@/services/clienteService';

export function ClienteDetalhePage() {
  const { id = '' } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [cliente, setCliente] = useState<ClienteDetalhe | null>(null);
  const [erro, setErro] = useState<string | null>(null);
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    let cancelado = false;
    void (async () => {
      try {
        const c = await clienteService.obterPorId(id);
        if (!cancelado) setCliente(c);
      } catch {
        if (!cancelado) setErro('Cliente não encontrado.');
      }
    })();
    return () => {
      cancelado = true;
    };
  }, [id]);

  const toggleStatus = useCallback(async () => {
    if (!cliente) return;
    setSalvando(true);
    setErro(null);
    try {
      const novo = await clienteService.alterarStatus(cliente.id, !cliente.ativo);
      setCliente(novo);
    } catch {
      setErro('Não foi possível alterar o status do cliente.');
    } finally {
      setSalvando(false);
    }
  }, [cliente]);

  if (erro && !cliente) {
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

  if (!cliente) {
    return <div className="px-8 py-8 text-sm text-zinc-500">Carregando…</div>;
  }

  return (
    <div className="space-y-6 px-8 py-8">
      <div className="flex items-center justify-between">
        <Button
          type="button"
          variant="outline"
          onClick={() => void navigate('/clientes')}
          className="h-9 rounded-full border-zinc-700/60 bg-transparent px-4 text-sm"
        >
          <ArrowLeft className="mr-1 h-4 w-4" /> Voltar
        </Button>
        <Button
          type="button"
          variant="outline"
          disabled={salvando}
          onClick={toggleStatus}
          className="h-9 rounded-full border-zinc-700/60 bg-transparent px-4 text-sm"
        >
          <Power className="mr-1 h-4 w-4" />
          {cliente.ativo ? 'Inativar cliente' : 'Reativar cliente'}
        </Button>
      </div>

      <Card className="border-zinc-800/60 bg-zinc-900/30">
        <CardHeader>
          <CardTitle className="flex items-center gap-3 text-xl text-zinc-100">
            {cliente.nome}
            <span
              className={
                cliente.ativo
                  ? 'rounded-full bg-green-500/10 px-2 py-0.5 text-[10px] font-bold tracking-[0.15em] text-green-400'
                  : 'rounded-full bg-zinc-700/40 px-2 py-0.5 text-[10px] font-bold tracking-[0.15em] text-zinc-500'
              }
            >
              {cliente.ativo ? 'ATIVO' : 'INATIVO'}
            </span>
          </CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-4 text-sm text-zinc-300 sm:grid-cols-3">
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
        </CardContent>
      </Card>

      {erro && (
        <p role="alert" className="text-sm text-red-400">
          {erro}
        </p>
      )}
    </div>
  );
}

function Campo({ label, valor }: { label: string; valor: string }) {
  return (
    <div>
      <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-zinc-500">{label}</p>
      <p className="mt-1 text-zinc-200">{valor}</p>
    </div>
  );
}
