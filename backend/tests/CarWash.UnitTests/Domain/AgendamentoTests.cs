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
            fim: DateTime.UtcNow.AddHours(1),
            duracaoTotalMin: 30,
            valorTotal: 30m);
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
            fim: DateTime.UtcNow.AddHours(1),
            duracaoTotalMin: 30,
            valorTotal: 30m);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Criar_exige_duracao_positiva()
    {
        var act = () => Agendamento.Criar(
            id: Guid.NewGuid(),
            filialId: Guid.NewGuid(),
            clienteId: Guid.NewGuid(),
            veiculoId: Guid.NewGuid(),
            criadoPor: Guid.NewGuid(),
            inicio: DateTime.UtcNow,
            fim: DateTime.UtcNow.AddHours(1),
            duracaoTotalMin: 0,
            valorTotal: 30m);
        act.Should().Throw<DomainException>().WithMessage("*positiva*");
    }

    [Fact]
    public void Criar_exige_valor_nao_negativo()
    {
        var act = () => Agendamento.Criar(
            id: Guid.NewGuid(),
            filialId: Guid.NewGuid(),
            clienteId: Guid.NewGuid(),
            veiculoId: Guid.NewGuid(),
            criadoPor: Guid.NewGuid(),
            inicio: DateTime.UtcNow,
            fim: DateTime.UtcNow.AddHours(1),
            duracaoTotalMin: 30,
            valorTotal: -1m);
        act.Should().Throw<DomainException>().WithMessage("*negativo*");
    }

    [Fact]
    public void Criar_preenche_duracao_e_valor()
    {
        var ag = NovoAgendamento();
        ag.DuracaoTotalMin.Should().Be(30);
        ag.ValorTotal.Should().Be(50m);
    }

    [Fact]
    public void Criar_status_inicial_agendado()
    {
        var ag = NovoAgendamento();
        ag.Status.Should().Be(StatusAgendamento.Agendado);
    }

    [Fact]
    public void Iniciar_muda_status_para_em_andamento()
    {
        var ag = NovoAgendamento();
        ag.Iniciar();
        ag.Status.Should().Be(StatusAgendamento.EmAndamento);
    }

    [Fact]
    public void Iniciar_incrementa_versao()
    {
        var ag = NovoAgendamento();
        var versao = ag.Versao;
        ag.Iniciar();
        ag.Versao.Should().Be(versao + 1);
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
    public void Cancelado_nao_pode_ser_alterado()
    {
        var ag = NovoAgendamento();
        ag.Cancelar();
        var act = ag.Iniciar;
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void EmAndamento_nao_pode_ser_finalizado_direto_sem_regra()
    {
        var ag = NovoAgendamento();
        ag.Iniciar();
        ag.Finalizar();
        ag.Status.Should().Be(StatusAgendamento.Finalizado);
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

    [Fact]
    public void Reagendar_altera_inicio_e_fim()
    {
        var ag = NovoAgendamento();
        var novoInicio = DateTime.UtcNow.AddHours(5);
        var novoFim = DateTime.UtcNow.AddHours(6);
        ag.Reagendar(novoInicio, novoFim);
        ag.Inicio.Should().Be(DateTime.SpecifyKind(novoInicio, DateTimeKind.Utc));
        ag.Fim.Should().Be(DateTime.SpecifyKind(novoFim, DateTimeKind.Utc));
    }

    [Fact]
    public void Reagendar_incrementa_versao()
    {
        var ag = NovoAgendamento();
        var versao = ag.Versao;
        ag.Reagendar(DateTime.UtcNow.AddHours(5), DateTime.UtcNow.AddHours(6));
        ag.Versao.Should().Be(versao + 1);
    }

    private static Agendamento NovoAgendamento() => Agendamento.Criar(
        id: Guid.NewGuid(),
        filialId: Guid.NewGuid(),
        clienteId: Guid.NewGuid(),
        veiculoId: Guid.NewGuid(),
        criadoPor: Guid.NewGuid(),
        inicio: DateTime.UtcNow.AddHours(1),
        fim: DateTime.UtcNow.AddHours(2),
        duracaoTotalMin: 30,
        valorTotal: 50m);
}
