import { http, HttpResponse } from 'msw';

import type { Responsavel, CriarResponsavelPayload } from '@/types/responsavel';

const servicosData = [
  {
    id: 'b7b83f06-5b92-4f9e-a0e4-9d10e0f31c2a',
    nome: 'Lavagem Simples',
    preco: 45.0,
    duracaoMin: 40,
    ativo: true,
    criadoEm: new Date(Date.now() - 100000000).toISOString(),
    atualizadoEm: new Date(Date.now() - 100000000).toISOString(),
  },
  {
    id: 'f3a4792c-62c1-4874-9843-c0d1c8f1e68b',
    nome: 'Lavagem Completa',
    preco: 79.9,
    duracaoMin: 90,
    ativo: true,
    criadoEm: new Date(Date.now() - 50000000).toISOString(),
    atualizadoEm: new Date(Date.now() - 50000000).toISOString(),
  },
  {
    id: '50e181c0-0f9c-44dc-8a6e-44d5c1817452',
    nome: 'Polimento',
    preco: 150.0,
    duracaoMin: 180,
    ativo: false,
    criadoEm: new Date(Date.now() - 20000000).toISOString(),
    atualizadoEm: new Date().toISOString(),
  },
];

interface ServicoBody {
  nome?: string;
  preco?: number | string;
  duracaoMin?: number | string;
}

const responsaveisPorCliente: Record<string, Responsavel[]> = {};

export const handlers = [
  http.get('*/api/v1/servicos', ({ request }) => {
    const url = new URL(request.url);
    const ativoStr = url.searchParams.get('ativo');
    let filtrados = [...servicosData];

    if (ativoStr !== null) {
      const ativo = ativoStr === 'true';
      filtrados = filtrados.filter((s) => s.ativo === ativo);
    }

    return HttpResponse.json({
      itens: filtrados,
      total: filtrados.length,
    });
  }),

  http.post('*/api/v1/servicos', async ({ request }) => {
    const body = (await request.json()) as ServicoBody;

    if (!body.nome || !body.preco || !body.duracaoMin) {
      return HttpResponse.json(
        {
          errors: { geral: ['Dados do serviço inválidos. Verifique os campos e tente novamente.'] },
        },
        { status: 400 },
      );
    }

    const existe = servicosData.some(
      (s) => s.nome.toLowerCase() === body.nome!.trim().toLowerCase(),
    );
    if (existe) {
      return HttpResponse.json(
        { message: 'Já existe serviço cadastrado com este nome.' },
        { status: 409 },
      );
    }

    const novo = {
      id: crypto.randomUUID(),
      nome: body.nome.trim(),
      preco: Number(body.preco),
      duracaoMin: Number(body.duracaoMin),
      ativo: true,
      criadoEm: new Date().toISOString(),
      atualizadoEm: new Date().toISOString(),
    };

    servicosData.unshift(novo);

    return HttpResponse.json(novo, { status: 201 });
  }),

  http.patch('*/api/v1/servicos/:id', async ({ request, params }) => {
    const { id } = params;
    const body = (await request.json()) as ServicoBody;

    const servicoIndex = servicosData.findIndex((s) => s.id === id);
    if (servicoIndex === -1) {
      return HttpResponse.json({ message: 'Serviço não encontrado.' }, { status: 404 });
    }

    if (!body.nome || !body.preco || !body.duracaoMin) {
      return HttpResponse.json(
        {
          errors: { geral: ['Dados do serviço inválidos. Verifique os campos e tente novamente.'] },
        },
        { status: 400 },
      );
    }

    const existe = servicosData.some(
      (s) => s.nome.toLowerCase() === body.nome!.trim().toLowerCase() && s.id !== id,
    );
    if (existe) {
      return HttpResponse.json(
        { message: 'Já existe serviço cadastrado com este nome.' },
        { status: 409 },
      );
    }

    const atual = servicosData[servicoIndex]!;

    servicosData[servicoIndex] = {
      ...atual,
      nome: body.nome.trim(),
      preco: Number(body.preco),
      duracaoMin: Number(body.duracaoMin),
      atualizadoEm: new Date().toISOString(),
    };

    return HttpResponse.json(servicosData[servicoIndex], { status: 200 });
  }),

  http.patch('*/api/v1/servicos/:id/status', async ({ request, params }) => {
    const { id } = params;
    const body = (await request.json()) as { ativo?: boolean };

    const servicoIndex = servicosData.findIndex((s) => s.id === id);
    if (servicoIndex === -1) {
      return HttpResponse.json({ message: 'Serviço não encontrado.' }, { status: 404 });
    }

    const atual = servicosData[servicoIndex]!;
    atual.ativo = Boolean(body.ativo);
    atual.atualizadoEm = new Date().toISOString();

    return HttpResponse.json(atual, { status: 200 });
  }),

  http.get('*/api/v1/clientes/:id/responsaveis', ({ params }) => {
    const { id } = params;
    const list = responsaveisPorCliente[id as string] ?? [];
    return HttpResponse.json(list);
  }),

  http.post('*/api/v1/clientes/:id/responsaveis', async ({ request, params }) => {
    const { id } = params;
    const body = (await request.json()) as CriarResponsavelPayload;

    if (!body.nome || !body.documento || !body.grauVinculo) {
      return HttpResponse.json({ message: 'Dados inválidos.' }, { status: 400 });
    }

    const list = responsaveisPorCliente[id as string] ?? [];
    const existeDoc = list.some((r) => r.documento === body.documento);
    if (existeDoc) {
      return HttpResponse.json(
        { message: 'Já existe responsável cadastrado com este documento.' },
        { status: 409 },
      );
    }

    const novoResp: Responsavel = {
      id: crypto.randomUUID(),
      nome: body.nome,
      documento: body.documento,
      email: body.email ?? null,
      telefone: body.telefone ?? null,
      grauVinculo: body.grauVinculo,
      criadoEm: new Date().toISOString(),
    };

    if (!responsaveisPorCliente[id as string]) {
      responsaveisPorCliente[id as string] = [];
    }
    responsaveisPorCliente[id as string]!.push(novoResp);

    return HttpResponse.json(novoResp, { status: 201 });
  }),
];
