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
        var versaoOriginal = ag.Versao;
        ag.Cancelar();
        ag.Versao.Should().Be(versaoOriginal + 1);
        ag.Status.Should().Be(StatusAgendamento.Cancelado);
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
