import { HttpResponse, http } from 'msw';
import { describe, expect, it } from 'vitest';

import { agendamentoService } from '@/services/agendamentoService';
import { server } from '@/test/mswServer';

/**
 * RF023/RF024 — guarda o contrato do dropdown de responsável.
 *
 * O bug de QA (MOMENTO 3) foi o backend não expor `GET /clientes/{id}/responsaveis`
 * (405), deixando o dropdown vazio. Estes testes fixam o contrato que o service
 * consome: array `[{ id, nome, documento }]` no caminho feliz e fallback `[]`
 * quando a API falha (a UI não deve quebrar).
 */
describe('agendamentoService.buscarResponsaveisPorCliente', () => {
  const clienteId = '11111111-1111-1111-1111-111111111111';

  it('mapeia a lista do backend para { id, nome, documento }', async () => {
    server.use(
      http.get(`/api/v1/clientes/${clienteId}/responsaveis`, () =>
        HttpResponse.json([
          {
            id: 'aaaaaaaa-0000-0000-0000-000000000001',
            nome: 'Ana Responsavel',
            documento: '39053344705',
            grauVinculo: 'RESPONSAVEL_FINANCEIRO',
            ativo: true,
          },
          {
            id: 'aaaaaaaa-0000-0000-0000-000000000002',
            nome: 'Bruno Responsavel',
            documento: '11144477735',
            grauVinculo: 'RESPONSAVEL_LEGAL',
            ativo: true,
          },
        ]),
      ),
    );

    const responsaveis = await agendamentoService.buscarResponsaveisPorCliente(clienteId);

    expect(responsaveis).toHaveLength(2);
    expect(responsaveis[0]).toEqual({
      id: 'aaaaaaaa-0000-0000-0000-000000000001',
      nome: 'Ana Responsavel',
      documento: '39053344705',
    });
    expect(responsaveis[1]?.id).toBe('aaaaaaaa-0000-0000-0000-000000000002');
  });

  it('retorna lista vazia quando o cliente não tem responsáveis', async () => {
    server.use(http.get(`/api/v1/clientes/${clienteId}/responsaveis`, () => HttpResponse.json([])));

    const responsaveis = await agendamentoService.buscarResponsaveisPorCliente(clienteId);

    expect(responsaveis).toEqual([]);
  });

  it('faz fallback para [] quando a API falha (não quebra a UI)', async () => {
    server.use(
      http.get(`/api/v1/clientes/${clienteId}/responsaveis`, () =>
        HttpResponse.json({ title: 'Erro interno' }, { status: 500 }),
      ),
    );

    const responsaveis = await agendamentoService.buscarResponsaveisPorCliente(clienteId);

    expect(responsaveis).toEqual([]);
  });
});
