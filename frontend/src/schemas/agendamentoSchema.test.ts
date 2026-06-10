import { describe, expect, it } from 'vitest';

import { IDS } from '@/test/handlers';

import { agendamentoSchema } from './agendamentoSchema';

/**
 * Testes do schema Zod (RF007). Validações de UX — o backend reconfirma tudo.
 */

function dataFutura(): string {
  const d = new Date(Date.now() + 24 * 60 * 60 * 1000);
  // Formato datetime-local: AAAA-MM-DDTHH:mm
  return d.toISOString().slice(0, 16);
}

function dataPassada(): string {
  const d = new Date(Date.now() - 24 * 60 * 60 * 1000);
  return d.toISOString().slice(0, 16);
}

const baseValida = {
  filialId: IDS.filial,
  clienteId: IDS.cliente,
  veiculoId: IDS.veiculo,
  responsavelId: '',
  inicio: dataFutura(),
  servicoIds: [IDS.servicoA],
  observacoes: '',
};

describe('agendamentoSchema', () => {
  it('aceita um payload válido completo', () => {
    const r = agendamentoSchema.safeParse(baseValida);
    expect(r.success).toBe(true);
  });

  it('rejeita quando a filial não é informada (RF019/RN010)', () => {
    const r = agendamentoSchema.safeParse({ ...baseValida, filialId: '' });
    expect(r.success).toBe(false);
    if (!r.success) {
      const issue = r.error.issues.find((i) => i.path[0] === 'filialId');
      expect(issue?.message).toMatch(/filial/i);
    }
  });

  it('rejeita lista de serviços vazia', () => {
    const r = agendamentoSchema.safeParse({ ...baseValida, servicoIds: [] });
    expect(r.success).toBe(false);
    if (!r.success) {
      const issue = r.error.issues.find((i) => i.path[0] === 'servicoIds');
      expect(issue?.message).toMatch(/ao menos um serviço/i);
    }
  });

  it('rejeita serviços duplicados', () => {
    const r = agendamentoSchema.safeParse({
      ...baseValida,
      servicoIds: [IDS.servicoA, IDS.servicoA],
    });
    expect(r.success).toBe(false);
    if (!r.success) {
      const issue = r.error.issues.find((i) => i.path[0] === 'servicoIds');
      expect(issue?.message).toMatch(/duplicad/i);
    }
  });

  it('rejeita início no passado', () => {
    const r = agendamentoSchema.safeParse({ ...baseValida, inicio: dataPassada() });
    expect(r.success).toBe(false);
    if (!r.success) {
      const issue = r.error.issues.find((i) => i.path[0] === 'inicio');
      expect(issue?.message).toMatch(/futura/i);
    }
  });

  it('rejeita observações acima de 500 caracteres', () => {
    const r = agendamentoSchema.safeParse({ ...baseValida, observacoes: 'x'.repeat(501) });
    expect(r.success).toBe(false);
    if (!r.success) {
      const issue = r.error.issues.find((i) => i.path[0] === 'observacoes');
      expect(issue?.message).toMatch(/500/);
    }
  });

  it('aceita responsável vazio (campo opcional)', () => {
    const r = agendamentoSchema.safeParse({ ...baseValida, responsavelId: '' });
    expect(r.success).toBe(true);
  });

  it('aceita agendamento sem observações logísticas', () => {
    const r = agendamentoSchema.safeParse({ ...baseValida, observacoesLogisticas: undefined });
    expect(r.success).toBe(true);
  });

  it('aceita observações logísticas vazias (campo opcional)', () => {
    const r = agendamentoSchema.safeParse({ ...baseValida, observacoesLogisticas: '' });
    expect(r.success).toBe(true);
  });

  it('aceita observações logísticas com exatamente 1000 caracteres', () => {
    const r = agendamentoSchema.safeParse({
      ...baseValida,
      observacoesLogisticas: 'x'.repeat(1000),
    });
    expect(r.success).toBe(true);
  });

  it('rejeita observações logísticas acima de 1000 caracteres', () => {
    const r = agendamentoSchema.safeParse({
      ...baseValida,
      observacoesLogisticas: 'x'.repeat(1001),
    });
    expect(r.success).toBe(false);
    if (!r.success) {
      const issue = r.error.issues.find((i) => i.path[0] === 'observacoesLogisticas');
      expect(issue?.message).toMatch(/1000/);
    }
  });

  it('aceita observações logísticas com quebras de linha', () => {
    const r = agendamentoSchema.safeParse({
      ...baseValida,
      observacoesLogisticas: 'Linha 1\nLinha 2\nLinha 3',
    });
    expect(r.success).toBe(true);
  });
});
