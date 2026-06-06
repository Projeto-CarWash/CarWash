using CarWash.Domain.Common;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Domain;

public class AgendamentoTests
{
    private static readonly Guid ResponsavelId = Guid.NewGuid();

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
            responsavelId: ResponsavelId);
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
            responsavelId: ResponsavelId);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Finalizado_nao_pode_voltar_ao_estado_anterior()
    {
        var ag = NovoAgendamento();
        ag.Iniciar();
        ag.Finalizar();
        var act = () => ag.Cancelar("Motivo de cancelamento teste", Guid.NewGuid());
        act.Should().Throw<DomainException>().WithMessage("*não pode ser cancelado*");
    }

    [Fact]
    public void Cancelar_incrementa_versao_e_registra_campos()
    {
        var ag = NovoAgendamento();
        int versaoOriginal = ag.Versao;
        var canceladoPor = Guid.NewGuid();
        ag.Cancelar("Cliente desistiu do serviço", canceladoPor);
        ag.Versao.Should().Be(versaoOriginal + 1);
        ag.Status.Should().Be(StatusAgendamento.Cancelado);
        ag.CanceladoPor.Should().Be(canceladoPor);
        ag.MotivoCancelamento.Should().Be("Cliente desistiu do serviço");
        ag.CanceladoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
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
            responsavelId: ResponsavelId,
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
            responsavelId: ResponsavelId,
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
            responsavelId: ResponsavelId,
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

    [Fact]
    public void Cancelar_exige_motivo_nao_vazio()
    {
        var ag = NovoAgendamento();
        var act = () => ag.Cancelar(string.Empty, Guid.NewGuid());
        act.Should().Throw<DomainException>().WithMessage("*Motivo*");
    }

    [Fact]
    public void Cancelar_exige_motivo_com_minimo_5_caracteres()
    {
        var ag = NovoAgendamento();
        var act = () => ag.Cancelar("abc", Guid.NewGuid());
        act.Should().Throw<DomainException>().WithMessage("*5*");
    }

    [Fact]
    public void Cancelar_exige_motivo_com_maximo_500_caracteres()
    {
        var ag = NovoAgendamento();
        var act = () => ag.Cancelar(new string('x', 501), Guid.NewGuid());
        act.Should().Throw<DomainException>().WithMessage("*500*");
    }

    [Fact]
    public void Cancelar_aceita_motivo_com_exatamente_5_caracteres()
    {
        var ag = NovoAgendamento();
        ag.Cancelar("abcde", Guid.NewGuid());
        ag.Status.Should().Be(StatusAgendamento.Cancelado);
    }

    [Fact]
    public void Cancelar_aceita_motivo_com_exatamente_500_caracteres()
    {
        var ag = NovoAgendamento();
        ag.Cancelar(new string('x', 500), Guid.NewGuid());
        ag.Status.Should().Be(StatusAgendamento.Cancelado);
    }

    [Fact]
    public void Cancelar_exige_usuario_nao_vazio()
    {
        var ag = NovoAgendamento();
        var act = () => ag.Cancelar("Motivo válido aqui", Guid.Empty);
        act.Should().Throw<DomainException>().WithMessage("*usuário*");
    }

    [Fact]
    public void Cancelar_trim_do_motivo()
    {
        var ag = NovoAgendamento();
        ag.Cancelar("  Motivo com espaços  ", Guid.NewGuid());
        ag.MotivoCancelamento.Should().Be("Motivo com espaços");
    }

    [Fact]
    public void Cancelado_nao_pode_ser_cancelado_novamente()
    {
        var ag = NovoAgendamento();
        ag.Cancelar("Primeiro cancelamento", Guid.NewGuid());
        var act = () => ag.Cancelar("Segundo cancelamento", Guid.NewGuid());
        act.Should().Throw<DomainException>().WithMessage("*já cancelado*");
    }

    [Fact]
    public void EmAndamento_nao_pode_ser_cancelado()
    {
        var ag = NovoAgendamento();
        ag.Iniciar();
        var act = () => ag.Cancelar("Tentativa de cancelar", Guid.NewGuid());
        act.Should().Throw<DomainException>().WithMessage("*em andamento*");
    }

    [Fact]
    public void Finalizado_nao_pode_ser_editado()
    {
        var ag = NovoAgendamento();
        ag.Iniciar();
        ag.Finalizar();
        var act = () => ag.Reagendar(DateTime.UtcNow.AddHours(3), DateTime.UtcNow.AddHours(4));
        act.Should().Throw<DomainException>().WithMessage("*finalizado*");
    }

    [Fact]
    public void Cancelado_nao_pode_ser_editado()
    {
        var ag = NovoAgendamento();
        ag.Cancelar("Cancelado para teste", Guid.NewGuid());
        var act = () => ag.Reagendar(DateTime.UtcNow.AddHours(3), DateTime.UtcNow.AddHours(4));
        act.Should().Throw<DomainException>().WithMessage("*cancelado*");
    }

    [Fact]
    public void EmAndamento_nao_pode_ser_editado()
    {
        var ag = NovoAgendamento();
        ag.Iniciar();
        var act = () => ag.Reagendar(DateTime.UtcNow.AddHours(3), DateTime.UtcNow.AddHours(4));
        act.Should().Throw<DomainException>().WithMessage("*não permite edição*");
    }

    [Fact]
    public void Iniciar_transiciona_para_em_andamento()
    {
        var ag = NovoAgendamento();
        int versaoOriginal = ag.Versao;
        ag.Iniciar();
        ag.Status.Should().Be(StatusAgendamento.EmAndamento);
        ag.Versao.Should().Be(versaoOriginal + 1);
    }

    [Fact]
    public void Iniciar_rejeita_se_ja_em_andamento()
    {
        var ag = NovoAgendamento();
        ag.Iniciar();
        var act = () => ag.Iniciar();
        act.Should().Throw<DomainException>().WithMessage("*agendado*");
    }

    [Fact]
    public void Iniciar_rejeita_se_finalizado()
    {
        var ag = NovoAgendamento();
        ag.Iniciar();
        ag.Finalizar();
        var act = () => ag.Iniciar();
        act.Should().Throw<DomainException>().WithMessage("*agendado*");
    }

    [Fact]
    public void Iniciar_rejeita_se_cancelado()
    {
        var ag = NovoAgendamento();
        ag.Cancelar("Motivo de cancelamento", Guid.NewGuid());
        var act = () => ag.Iniciar();
        act.Should().Throw<DomainException>().WithMessage("*agendado*");
    }

    [Fact]
    public void Agendado_pode_ser_cancelado()
    {
        var ag = NovoAgendamento();
        var canceladoPor = Guid.NewGuid();
        ag.Cancelar("Cliente solicitou cancelamento", canceladoPor);
        ag.Status.Should().Be(StatusAgendamento.Cancelado);
        ag.CanceladoPor.Should().Be(canceladoPor);
        ag.CanceladoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Agendado_pode_ser_reagendado()
    {
        var ag = NovoAgendamento();
        var novoInicio = DateTime.UtcNow.AddHours(5);
        var novoFim = novoInicio.AddHours(1);
        ag.Reagendar(novoInicio, novoFim);
        ag.Inicio.Should().BeCloseTo(novoInicio, TimeSpan.FromSeconds(1));
        ag.Fim.Should().BeCloseTo(novoFim, TimeSpan.FromSeconds(1));
    }

    private static Agendamento NovoAgendamento() => Agendamento.Criar(
        id: Guid.NewGuid(),
        filialId: Guid.NewGuid(),
        clienteId: Guid.NewGuid(),
        veiculoId: Guid.NewGuid(),
        criadoPor: Guid.NewGuid(),
        inicio: DateTime.UtcNow.AddHours(1),
        fim: DateTime.UtcNow.AddHours(2),
        responsavelId: ResponsavelId);
}
