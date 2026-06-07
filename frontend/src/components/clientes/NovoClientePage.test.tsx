import { fireEvent, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { renderComProviders } from '@/test/renderComProviders';

import { NovoClientePage } from './NovoClientePage';

describe('NovoClientePage — Limpar Formulário', () => {
  // Regressão: o antigo guard isResettingRef + requestAnimationFrame ficava preso
  // quando o callback do rAF não disparava (aba em segundo plano / frame descartado),
  // deixando o ref em `true` para sempre e travando o botão Limpar.
  beforeEach(() => {
    vi.stubGlobal('requestAnimationFrame', () => 1);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('limpa o formulário e mantém os campos editáveis com cliques repetidos', async () => {
    const user = userEvent.setup();
    renderComProviders(<NovoClientePage />);

    const nome = screen.getByLabelText(/nome completo/i);
    const data = screen.getByLabelText(/data de nascimento/i);
    const limpar = screen.getByRole('button', { name: /limpar formul/i });

    await user.type(nome, 'Maria Silva');
    expect(nome).toHaveValue('Maria Silva');

    // 3 cliques seguidos (cenário exato do bug reportado)
    fireEvent.click(limpar);
    fireEvent.click(limpar);
    fireEvent.click(limpar);
    expect(nome).toHaveValue('');

    // Campos continuam editáveis após múltiplos cliques
    await user.type(nome, 'Joao Souza');
    await user.type(data, '01011990');
    expect(nome).toHaveValue('Joao Souza');
    expect(data).toHaveValue('01/01/1990');

    // E o botão Limpar continua funcionando
    fireEvent.click(limpar);
    expect(nome).toHaveValue('');
    expect(data).toHaveValue('');
  });
});
