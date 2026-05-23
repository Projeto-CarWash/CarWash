import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

import { RevisaoAgendamento } from './RevisaoAgendamento';

import type { ResumoConfirmacao } from '@/types/agendamento';

/**
 * Testes unitários do componente da etapa de revisão do RF015 (card 133).
 *
 * <p>Cobertura: renderização do resumo derivado pelo backend, callbacks de
 * confirmar/editar, bloqueio anti-duplo-clique e exibição de erro.</p>
 */

function resumoBase(): ResumoConfirmacao {
  return {
    filial: { id: 'f1', nome: 'Filial Centro' },
    cliente: { id: 'c1', nome: 'João da Silva', documento: '123.456.789-00' },
    veiculo: { id: 'v1', placa: 'ABC1D23', modelo: 'Fiat Uno', cor: 'Prata' },
    servicos: [
      { id: 's1', nome: 'Lavagem simples', duracaoMin: 30, preco: 50 },
      { id: 's2', nome: 'Enceramento', duracaoMin: 60, preco: 75 },
    ],
    inicio: '2099-01-01T14:00:00.000Z',
    fim: '2099-01-01T15:30:00.000Z',
    duracaoTotalMin: 90,
    valorTotal: 125,
    observacoes: null,
    hashResumo: 'hash-abc',
  };
}

describe('RevisaoAgendamento', () => {
  it('renderiza os dados do resumo retornado pelo backend', () => {
    render(
      <RevisaoAgendamento
        resumo={resumoBase()}
        onConfirmar={vi.fn()}
        onEditar={vi.fn()}
        confirmando={false}
      />,
    );

    expect(screen.getByText('Filial Centro')).toBeInTheDocument();
    expect(screen.getByText('João da Silva')).toBeInTheDocument();
    expect(screen.getByText('123.456.789-00')).toBeInTheDocument();
    expect(screen.getByText('ABC1D23')).toBeInTheDocument();
    expect(screen.getByText(/Fiat Uno · Prata/)).toBeInTheDocument();
    expect(screen.getByText('Lavagem simples')).toBeInTheDocument();
    expect(screen.getByText('Enceramento')).toBeInTheDocument();
  });

  it('exibe os totais derivados pelo servidor', () => {
    render(
      <RevisaoAgendamento
        resumo={resumoBase()}
        onConfirmar={vi.fn()}
        onEditar={vi.fn()}
        confirmando={false}
      />,
    );

    expect(screen.getByTestId('revisao-duracao')).toHaveTextContent('1 h 30 min');
    expect(screen.getByTestId('revisao-valor')).toHaveTextContent('R$ 125,00');
  });

  it('renderiza as observações quando presentes', () => {
    const resumo = { ...resumoBase(), observacoes: 'Cliente pediu cuidado com o para-choque.' };
    render(
      <RevisaoAgendamento
        resumo={resumo}
        onConfirmar={vi.fn()}
        onEditar={vi.fn()}
        confirmando={false}
      />,
    );

    expect(screen.getByText(/cliente pediu cuidado com o para-choque/i)).toBeInTheDocument();
  });

  it('omite a seção de observações quando nulas', () => {
    render(
      <RevisaoAgendamento
        resumo={resumoBase()}
        onConfirmar={vi.fn()}
        onEditar={vi.fn()}
        confirmando={false}
      />,
    );

    expect(screen.queryByText('Observações')).not.toBeInTheDocument();
  });

  it('chama onConfirmar ao clicar em "Confirmar agendamento"', async () => {
    const onConfirmar = vi.fn();
    const user = userEvent.setup();
    render(
      <RevisaoAgendamento
        resumo={resumoBase()}
        onConfirmar={onConfirmar}
        onEditar={vi.fn()}
        confirmando={false}
      />,
    );

    await user.click(screen.getByRole('button', { name: /confirmar agendamento/i }));
    expect(onConfirmar).toHaveBeenCalledTimes(1);
  });

  it('chama onEditar ao clicar em "Editar"', async () => {
    const onEditar = vi.fn();
    const user = userEvent.setup();
    render(
      <RevisaoAgendamento
        resumo={resumoBase()}
        onConfirmar={vi.fn()}
        onEditar={onEditar}
        confirmando={false}
      />,
    );

    await user.click(screen.getByRole('button', { name: /editar/i }));
    expect(onEditar).toHaveBeenCalledTimes(1);
  });

  it('desabilita os botões enquanto a confirmação está em andamento', () => {
    render(
      <RevisaoAgendamento
        resumo={resumoBase()}
        onConfirmar={vi.fn()}
        onEditar={vi.fn()}
        confirmando
      />,
    );

    expect(screen.getByRole('button', { name: /confirmando/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /editar/i })).toBeDisabled();
  });

  it('exibe o banner de erro quando há erro de confirmação', () => {
    render(
      <RevisaoAgendamento
        resumo={resumoBase()}
        onConfirmar={vi.fn()}
        onEditar={vi.fn()}
        confirmando={false}
        erro="O horário não está mais disponível para este veículo."
      />,
    );

    const alerta = screen.getByRole('alert');
    expect(alerta).toHaveTextContent(/não está mais disponível/i);
  });
});
