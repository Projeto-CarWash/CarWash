import { AxiosError, AxiosHeaders } from 'axios';
import { describe, expect, it } from 'vitest';

import { tratarErroApi } from './apiError';

import type { ProblemDetails } from '@/types/auth';

/**
 * Cria um AxiosError com resposta de status e ProblemDetails arbitrários.
 */
function erroComResposta(status: number, data: ProblemDetails): AxiosError<ProblemDetails> {
  const erro = new AxiosError<ProblemDetails>('falha');
  erro.response = {
    status,
    data,
    statusText: '',
    headers: {},
    config: { headers: new AxiosHeaders() },
  };
  return erro;
}

describe('tratarErroApi', () => {
  it('usa o title do backend em conflito de agenda (409 / RN011)', () => {
    const info = tratarErroApi(
      erroComResposta(409, { title: 'Veículo já agendado neste horário.' }),
    );
    expect(info.status).toBe(409);
    expect(info.mensagem).toBe('Veículo já agendado neste horário.');
  });

  it('usa o title do backend em recurso inativo (422)', () => {
    const info = tratarErroApi(erroComResposta(422, { title: 'A filial está desativada.' }));
    expect(info.status).toBe(422);
    expect(info.mensagem).toBe('A filial está desativada.');
  });

  it('extrai errors por campo em validação 400', () => {
    const info = tratarErroApi(
      erroComResposta(400, {
        title: 'Dados inválidos',
        errors: { servicoIds: ['Selecione ao menos um serviço.'], inicio: ['Data inválida.'] },
      }),
    );
    expect(info.status).toBe(400);
    expect(info.errorsPorCampo.servicoIds).toBe('Selecione ao menos um serviço.');
    expect(info.errorsPorCampo.inicio).toBe('Data inválida.');
  });

  it('usa mensagem genérica em 401 ignorando o title', () => {
    const info = tratarErroApi(erroComResposta(401, { title: 'detalhe interno' }));
    expect(info.mensagem).toMatch(/sessão expirada/i);
  });

  it('usa mensagem genérica em 500', () => {
    const info = tratarErroApi(erroComResposta(500, {}));
    expect(info.mensagem).toMatch(/não foi possível/i);
  });

  it('trata erro de rede sem resposta', () => {
    const erro = new AxiosError('rede');
    erro.code = 'ERR_NETWORK';
    const info = tratarErroApi(erro);
    expect(info.status).toBeNull();
    expect(info.mensagem).toMatch(/conexão/i);
  });

  it('trata erro não-axios como genérico', () => {
    const info = tratarErroApi(new Error('boom'));
    expect(info.status).toBeNull();
    expect(info.mensagem).toMatch(/não foi possível/i);
  });
});
