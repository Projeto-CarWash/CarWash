import { z } from 'zod';

/**
 * Schema do formulário de criação de agendamento (RF007).
 *
 * <p>Espelha o contrato fechado pelo arquiteto para `POST /api/v1/agendamentos`:
 * ids obrigatórios, ao menos 1 serviço sem duplicatas, início futuro e
 * observações com no máximo 500 caracteres.</p>
 *
 * <p>O backend é a fonte de verdade — este schema entrega feedback rápido de
 * UX, mas regras críticas (RN004/RN006/RN010/RN011) são reconfirmadas
 * server-side (DAT §4.2). Em especial, conflito de agenda do veículo (409) e
 * recurso inativo (422) só o backend detecta de forma confiável.</p>
 */

const idObrigatorio = (mensagem: string) =>
  z
    .string({ message: mensagem })
    .trim()
    .min(1, mensagem)
    .refine((val) => z.uuid().safeParse(val).success, {
      message: 'Identificador inválido.',
    });

export const agendamentoSchema = z.object({
  filialId: idObrigatorio('Selecione a filial (RF019/RN010).'),

  clienteId: idObrigatorio('Selecione o cliente.'),

  veiculoId: idObrigatorio('Selecione o veículo do cliente.'),

  /** Opcional — string vazia equivale a "sem responsável" e vira null no envio. */
  responsavelId: z
    .string()
    .trim()
    .refine((val) => val === '' || z.uuid().safeParse(val).success, {
      message: 'Responsável inválido.',
    }),

  servicoIds: z
    .array(z.uuid('Serviço inválido.'))
    .min(1, 'Selecione ao menos um serviço.')
    .refine((ids) => new Set(ids).size === ids.length, {
      message: 'Há serviços duplicados na seleção.',
    }),

  /**
   * Início no formato `datetime-local` (`AAAA-MM-DDTHH:mm`, hora local).
   * Convertido para ISO-8601 UTC com `Z` apenas no submit.
   */
  inicio: z
    .string()
    .min(1, 'Informe a data e a hora de início.')
    .refine((val) => !Number.isNaN(new Date(val).getTime()), {
      message: 'Data ou hora inválida.',
    })
    .refine((val) => new Date(val).getTime() > Date.now(), {
      message: 'O início deve ser uma data futura.',
    }),

  observacoes: z.string().max(500, 'Observações devem ter no máximo 500 caracteres.').optional(),

  /**
   * Observações logísticas opcionais — máx. 1000 caracteres.
   * Campo adicional para registro de informações complementares de logística.
   */
  observacoesLogisticas: z
    .string()
    .max(1000, 'Observação logística deve ter no máximo 1000 caracteres.')
    .optional(),
});

export type AgendamentoFormData = z.infer<typeof agendamentoSchema>;
