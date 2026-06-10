import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { describe, expect, it, vi } from 'vitest';

import { FilialFormPage } from '@/pages/Filiais/FilialFormPage';
import { server } from '@/test/mswServer';
import { renderComProviders } from '@/test/renderComProviders';

/**
 * Testes de integração do formulário de cadastro de filial (RF017/RF018).
 *
 * Cobertura: validação local por campo, normalização do payload (código/UF
 * maiúsculos, CEP/CNPJ só dígitos, endereço estruturado), sucesso (201) e
 * conflito (409).
 */

async function preencherFormularioValido(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText(/nome da filial/i), 'Unidade Centro');
  await user.type(screen.getByLabelText(/código da filial/i), 'centro01');
  await user.type(screen.getByLabelText(/células ativas/i), '4');
  await user.type(screen.getByLabelText(/cep/i), '01001000');
  await user.type(screen.getByLabelText(/número/i), '1000');
  await user.type(screen.getByLabelText(/logradouro/i), 'Av. Principal');
  await user.type(screen.getByLabelText(/bairro/i), 'Centro');
  await user.type(screen.getByLabelText(/cidade/i), 'São Paulo');
  await user.type(screen.getByLabelText(/^uf$/i), 'sp');
}

describe('FilialFormPage (RF017/RF018)', () => {
  it('mantém o botão Salvar desabilitado enquanto o formulário é inválido', () => {
    renderComProviders(<FilialFormPage />);

    // RF: desabilitar enquanto houver erros de validação / formulário incompleto.
    expect(screen.getByRole('button', { name: /salvar filial/i })).toBeDisabled();
  });

  it('exibe mensagens de validação por campo ao informar valores inválidos', async () => {
    const user = userEvent.setup();
    renderComProviders(<FilialFormPage />);

    await user.type(screen.getByLabelText(/nome da filial/i), 'ab');
    await user.type(screen.getByLabelText(/células ativas/i), '200');

    expect(
      await screen.findByText('Nome da filial deve ter entre 3 e 120 caracteres.'),
    ).toBeInTheDocument();
    expect(screen.getByText('Células ativas deve estar entre 1 e 100.')).toBeInTheDocument();
  });

  it('bloqueia caracteres não inteiros no campo de células ativas (e, +, -, ., ,)', async () => {
    const user = userEvent.setup();
    renderComProviders(<FilialFormPage />);

    const campo = screen.getByLabelText(/células ativas/i);
    await user.type(campo, '1e2E.3+-,');

    // Apenas os dígitos sobrevivem ao bloqueio de tecla / higienização.
    expect(campo).toHaveValue('123');
  });

  it('envia o payload normalizado (endereço estruturado) e exibe sucesso (201)', async () => {
    const recebido = vi.fn();
    server.use(
      http.post('/api/v1/filiais', async ({ request }) => {
        recebido(await request.json());
        return HttpResponse.json(
          { id: 'nova-filial-id', mensagem: 'Filial cadastrada com sucesso.', traceId: 't' },
          { status: 201 },
        );
      }),
    );

    const user = userEvent.setup();
    renderComProviders(<FilialFormPage />);

    await preencherFormularioValido(user);
    await user.click(screen.getByRole('button', { name: /salvar filial/i }));

    expect(await screen.findByText(/filial cadastrada com sucesso/i)).toBeInTheDocument();
    expect(recebido).toHaveBeenCalledWith({
      nome: 'Unidade Centro',
      codigo: 'CENTRO01',
      cnpj: null,
      celulasAtivas: 4,
      endereco: {
        cep: '01001000',
        logradouro: 'Av. Principal',
        numero: '1000',
        complemento: null,
        bairro: 'Centro',
        cidade: 'São Paulo',
        uf: 'SP',
      },
    });
  });

  it('destaca os campos conflitantes ao receber 409', async () => {
    server.use(
      http.post('/api/v1/filiais', () => HttpResponse.json({ title: 'Conflito' }, { status: 409 })),
    );

    const user = userEvent.setup();
    renderComProviders(<FilialFormPage />);

    await preencherFormularioValido(user);
    await user.click(screen.getByRole('button', { name: /salvar filial/i }));

    await waitFor(() => {
      expect(
        screen.getByText(/já existe filial cadastrada com este identificador/i),
      ).toBeInTheDocument();
    });
    expect(screen.getByText(/já existe filial cadastrada com este código/i)).toBeInTheDocument();
    expect(screen.getByText(/já existe filial cadastrada com este cnpj/i)).toBeInTheDocument();
  });
});
