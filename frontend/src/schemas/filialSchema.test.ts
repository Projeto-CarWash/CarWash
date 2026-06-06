import { describe, expect, it } from 'vitest';

import { MENSAGENS_CELULAS_ATIVAS, celulasAtivasSchema } from '@/schemas/filialSchema';

/**
 * Unidade do campo "células ativas" (RF018): garante mensagens distintas por
 * caso e que a saída é sempre um inteiro (`number`) — o payload nunca trafega
 * string. A validação canônica permanece no backend (RAT03).
 */
describe('celulasAtivasSchema (RF018)', () => {
  function primeiroErro(valor: unknown): string | undefined {
    const r = celulasAtivasSchema.safeParse(valor);
    return r.success ? undefined : r.error.issues[0]?.message;
  }

  it('exige o campo quando vazio, nulo ou ausente', () => {
    expect(primeiroErro('')).toBe(MENSAGENS_CELULAS_ATIVAS.obrigatorio);
    expect(primeiroErro('   ')).toBe(MENSAGENS_CELULAS_ATIVAS.obrigatorio);
    expect(primeiroErro(undefined)).toBe(MENSAGENS_CELULAS_ATIVAS.obrigatorio);
    expect(primeiroErro(null)).toBe(MENSAGENS_CELULAS_ATIVAS.obrigatorio);
  });

  it('rejeita texto e decimais com a mensagem de tipo inválido', () => {
    expect(primeiroErro('abc')).toBe(MENSAGENS_CELULAS_ATIVAS.tipo);
    expect(primeiroErro(4.5)).toBe(MENSAGENS_CELULAS_ATIVAS.tipo);
  });

  it('rejeita valores fora da faixa 1–100', () => {
    expect(primeiroErro(0)).toBe(MENSAGENS_CELULAS_ATIVAS.faixa);
    expect(primeiroErro(-3)).toBe(MENSAGENS_CELULAS_ATIVAS.faixa);
    expect(primeiroErro(101)).toBe(MENSAGENS_CELULAS_ATIVAS.faixa);
  });

  it('aceita inteiros válidos e devolve number (não string)', () => {
    const r = celulasAtivasSchema.safeParse('4');
    expect(r.success).toBe(true);
    if (r.success) {
      expect(r.data).toBe(4);
      expect(typeof r.data).toBe('number');
    }

    expect(celulasAtivasSchema.safeParse(1).success).toBe(true);
    expect(celulasAtivasSchema.safeParse(100).success).toBe(true);
  });
});
