using CarWash.Application.Agenda.Common;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Application.Agenda;

public class StatusAgendaMapperTests
{
    [Theory]
    [InlineData("AGENDADO", "agendado")]
    [InlineData("CONCLUIDO", "finalizado")]
    [InlineData("CANCELADO", "cancelado")]
    public void ParaDb_mapeia_os_tres_status_persistidos(string api, string db)
    {
        StatusAgendaMapper.ParaDb(api).Should().Be(db);
    }

    [Fact]
    public void ParaDb_e_case_insensitive()
    {
        StatusAgendaMapper.ParaDb("agendado").Should().Be("agendado");
        StatusAgendaMapper.ParaDb("Concluido").Should().Be("finalizado");
    }

    [Fact]
    public void ParaDb_de_em_andamento_retorna_null_curto_circuito()
    {
        // L1: EM_ANDAMENTO não tem correspondente persistido.
        StatusAgendaMapper.ParaDb("EM_ANDAMENTO").Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("INEXISTENTE")]
    public void ParaDb_de_valor_invalido_ou_vazio_retorna_null(string? valor)
    {
        StatusAgendaMapper.ParaDb(valor).Should().BeNull();
    }

    [Theory]
    [InlineData("agendado", "AGENDADO")]
    [InlineData("finalizado", "CONCLUIDO")]
    [InlineData("cancelado", "CANCELADO")]
    public void ParaApi_mapeia_para_uppercase_do_contrato(string db, string api)
    {
        StatusAgendaMapper.ParaApi(db).Should().Be(api);
    }

    [Fact]
    public void ParaApi_de_status_persistido_invalido_lanca()
    {
        var act = () => StatusAgendaMapper.ParaApi("em_andamento");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("AGENDADO")]
    [InlineData("EM_ANDAMENTO")]
    [InlineData("CONCLUIDO")]
    [InlineData("CANCELADO")]
    [InlineData("agendado")]
    [InlineData("em_andamento")]
    public void EhStatusApiValido_aceita_os_quatro_valores_do_contrato(string status)
    {
        StatusAgendaMapper.EhStatusApiValido(status).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("FINALIZADO")]
    [InlineData("PENDENTE")]
    public void EhStatusApiValido_rejeita_valores_fora_do_contrato(string? status)
    {
        StatusAgendaMapper.EhStatusApiValido(status).Should().BeFalse();
    }

    [Theory]
    [InlineData("EM_ANDAMENTO")]
    [InlineData("em_andamento")]
    [InlineData("  EM_ANDAMENTO  ")]
    public void EhEmAndamento_reconhece_o_valor_curto_circuito(string status)
    {
        StatusAgendaMapper.EhEmAndamento(status).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("AGENDADO")]
    [InlineData("CANCELADO")]
    public void EhEmAndamento_falso_para_demais_valores(string? status)
    {
        StatusAgendaMapper.EhEmAndamento(status).Should().BeFalse();
    }
}
