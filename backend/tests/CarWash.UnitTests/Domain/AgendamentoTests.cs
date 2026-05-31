using CarWash.Domain.Common;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Domain;

public class AgendamentoTests
{
    [Fact]
    public void Criar_exige_filial()
    {
        var act = () => Agendamento.Criar(
            id: Guid.NewGuid(),
            filialId: Guid.Empty,
            clienteId: Guid.NewGuid(),
            veiculoId: Guid.NewGuid(),
            criadoPor: Guid.NewGuid(),
            inicio: DateTime.UtcNow,
            fim: DateTime.UtcNow.AddHours(1));
        act.Should().Throw<DomainException>().WithMessage("*RN010*");
    }

    [Fact]
    public void Criar_exige_inicio_menor_que_fim()
    {
        var act = () => Agendamento.Criar(
            id: Guid.NewGuid(),
            filialId: Guid.NewGuid(),
            clienteId: Guid.NewGuid(),
            veiculoId: Guid.NewGuid(),
            criadoPor: Guid.NewGuid(),
            inicio: DateTime.UtcNow.AddHours(2),
            fim: DateTime.UtcNow.AddHours(1));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Finalizado_nao_pode_voltar_ao_estado_anterior()
    {
        var ag = NovoAgendamento();
        ag.Finalizar();
        var act = ag.Cancelar;
        act.Should().Throw<DomainException>().WithMessage("*RN004*");
    }

    [Fact]
    public void Cancelar_incrementa_versao_para_concorrencia_otimista()
    {
        var ag = NovoAgendamento();
        int versaoOriginal = ag.Versao;
        ag.Cancelar();
        ag.Versao.Should().Be(versaoOriginal + 1);
        ag.Status.Should().Be(StatusAgendamento.Cancelado);
    }

    [Fact]
    public void Criar_persiste_totais_informados()
    {
        var ag = Agendamento.Criar(
            id: Guid.NewGuid(),
            filialId: Guid.NewGuid(),
            clienteId: Guid.NewGuid(),
            veiculoId: Guid.NewGuid(),
            criadoPor: Guid.NewGuid(),
            inicio: DateTime.UtcNow.AddHours(1),
            fim: DateTime.UtcNow.AddHours(2),
            duracaoTotalMin: 90,
            valorTotal: 135.50m);

        ag.DuracaoTotalMin.Should().Be(90);
        ag.ValorTotal.Should().Be(135.50m);
    }

    [Fact]
    public void Criar_rejeita_duracao_total_negativa()
    {
        var act = () => Agendamento.Criar(
            id: Guid.NewGuid(),
            filialId: Guid.NewGuid(),
            clienteId: Guid.NewGuid(),
            veiculoId: Guid.NewGuid(),
            criadoPor: Guid.NewGuid(),
            inicio: DateTime.UtcNow.AddHours(1),
            fim: DateTime.UtcNow.AddHours(2),
            duracaoTotalMin: -1);
        act.Should().Throw<DomainException>().WithMessage("*Duração total*");
    }

    [Fact]
    public void Criar_rejeita_valor_total_negativo()
    {
        var act = () => Agendamento.Criar(
            id: Guid.NewGuid(),
            filialId: Guid.NewGuid(),
            clienteId: Guid.NewGuid(),
            veiculoId: Guid.NewGuid(),
            criadoPor: Guid.NewGuid(),
            inicio: DateTime.UtcNow.AddHours(1),
            fim: DateTime.UtcNow.AddHours(2),
            valorTotal: -0.01m);
        act.Should().Throw<DomainException>().WithMessage("*Valor total*");
    }

    [Fact]
    public void DefinirTotais_atualiza_duracao_e_valor()
    {
        var ag = NovoAgendamento();
        ag.DefinirTotais(120, 200m);
        ag.DuracaoTotalMin.Should().Be(120);
        ag.ValorTotal.Should().Be(200m);
    }

    [Fact]
    public void DefinirTotais_rejeita_valores_negativos()
    {
        var ag = NovoAgendamento();
        var actDuracao = () => ag.DefinirTotais(-1, 0m);
        var actValor = () => ag.DefinirTotais(0, -1m);
        actDuracao.Should().Throw<DomainException>();
        actValor.Should().Throw<DomainException>();
    }

    private static Agendamento NovoAgendamento() => Agendamento.Criar(
        id: Guid.NewGuid(),
        filialId: Guid.NewGuid(),
        clienteId: Guid.NewGuid(),
        veiculoId: Guid.NewGuid(),
        criadoPor: Guid.NewGuid(),
        inicio: DateTime.UtcNow.AddHours(1),
        fim: DateTime.UtcNow.AddHours(2));
}
