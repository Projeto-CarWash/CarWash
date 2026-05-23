import axios from 'axios';

import type { ProblemDetails } from '@/types/auth';

/**
 * Resultado normalizado do tratamento de um erro de API.
 *
 * <p>`mensagem` é sempre apresentável ao usuário (pt-BR). `errorsPorCampo`
 * traz, quando houver, as mensagens de validação por campo (HTTP 400) para
 * destacar inputs específicos no formulário.</p>
 */
export interface ApiErrorInfo {
  status: number | null;
  mensagem: string;
  errorsPorCampo: Record<string, string>;
  correlationId?: string;
}

/** Mensagens genéricas por status. O `title` do ProblemDetails tem prioridade. */
const MENSAGENS_PADRAO: Record<number, string> = {
  400: 'Dados do agendamento inválidos. Verifique os campos destacados.',
  401: 'Sessão expirada. Faça login novamente.',
  403: 'Você não possui permissão para criar agendamentos.',
  404: 'Um dos recursos selecionados não foi encontrado. Atualize a página e tente novamente.',
  409: 'Este veículo já possui um agendamento que conflita com o horário escolhido (RN011).',
  422: 'Há um recurso desativado na seleção (filial, cliente, veículo ou serviço).',
  500: 'Não foi possível concluir o agendamento agora. Tente novamente.',
};

/**
 * Converte um erro qualquer (axios ou não) em informação apresentável.
 *
 * <p>Para 4xx de negócio (404/409/422) prioriza o `title` do ProblemDetails,
 * que vem contextualizado pelo backend. Para 400, extrai `errors` por campo.
 * Para 401/403/500 ou erro de rede, usa mensagem genérica.</p>
 */
export function tratarErroApi(error: unknown): ApiErrorInfo {
  if (!axios.isAxiosError<ProblemDetails>(error)) {
    return { status: null, mensagem: MENSAGENS_PADRAO[500]!, errorsPorCampo: {} };
  }

  if (!error.response) {
    const mensagem =
      error.code === 'ECONNABORTED' || error.code === 'ERR_NETWORK'
        ? 'Não foi possível contatar o servidor. Verifique sua conexão.'
        : MENSAGENS_PADRAO[500]!;
    return { status: null, mensagem, errorsPorCampo: {} };
  }

  const status = error.response.status;
  const problem = error.response.data;
  const title = typeof problem?.title === 'string' ? problem.title : undefined;
  const correlationId = problem?.correlationId;

  const errorsPorCampo: Record<string, string> = {};
  if (status === 400 && problem?.errors) {
    for (const [campo, mensagens] of Object.entries(problem.errors)) {
      const primeira = mensagens[0];
      if (primeira) {
        errorsPorCampo[campo] = primeira;
      }
    }
  }

  // 404/409/422 são erros de negócio — o título do backend é o mais informativo.
  const usaTitleDoBackend = status === 404 || status === 409 || status === 422;
  const mensagem =
    (usaTitleDoBackend ? title : undefined) ??
    MENSAGENS_PADRAO[status] ??
    title ??
    MENSAGENS_PADRAO[500]!;

  return { status, mensagem, errorsPorCampo, correlationId };
}
