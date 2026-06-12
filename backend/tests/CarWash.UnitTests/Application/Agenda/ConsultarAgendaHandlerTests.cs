using System.Globalization;
using CarWash.Application.Agenda.Common;
using CarWash.Application.Agenda.Consultar;
using CarWash.Application.Agenda.Persistence;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Agenda;

public class ConsultarAgendaHandlerTests
{
    private static readonly DateTime Base = new(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly Guid FilialId = Guid.NewGuid();

    private readonly IAgendaRepository _repo = Substitute.For<IAgendaRepository>();

    [Fact]
    public async Task Em_andamento_filtra_no_repositorio_pelo_status_persistido()
    {
        // RF010/RF013: em_andamento passou a existir persistido (iniciar/finalizar),
        // então o filtro EM_ANDAMENTO resolve no banco em vez de curto-circuitar.
        _repo.ConsultarAsync(
                Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AgendaProjecao>)new List<AgendaProjecao>());

        var handler = new ConsultarAgendaHandler(_repo);

        var resposta = await handler.HandleAsync(
            QueryValida() with { Status = "EM_ANDAMENTO" },
            CancellationToken.None);

        resposta.Data.Should().BeEmpty();
        await _repo.Received(1).ConsultarAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), "em_andamento", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Lista_vazia_do_repositorio_devolve_mensagem_de_periodo_vazio()
    {
        _repo.ConsultarAsync(
                Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AgendaProjecao>)new List<AgendaProjecao>());

        var handler = new ConsultarAgendaHandler(_repo);
        var resposta = await handler.HandleAsync(QueryValida(), CancellationToken.None);

        resposta.Data.Should().BeEmpty();
        resposta.Message.Should().Be("Nenhum evento encontrado para o período selecionado.");
        resposta.TraceId.Should().Be("trace-1");
    }

    [Fact]
    public async Task Formato_simples_projeta_oito_campos_e_mapeia_status()
    {
        var projecao = ProjecaoCom(2);
        ConfigurarRepo(projecao);

        var handler = new ConsultarAgendaHandler(_repo);
        var resposta = await handler.HandleAsync(
            QueryValida() with { Formato = "simples" },
            CancellationToken.None);

        resposta.Message.Should().Be("Agenda consultada com sucesso.");
        resposta.Data.Should().ContainSingle();
        var item = resposta.Data[0].Should().BeOfType<AgendaItemSimplesResponse>().Subject;
        item.AgendamentoId.Should().Be(projecao.AgendamentoId);
        item.Inicio.Should().Be(projecao.Inicio);
        item.Fim.Should().Be(projecao.Fim);
        item.Status.Should().Be("AGENDADO");
        item.ClienteNome.Should().Be("Maria Souza");
        item.VeiculoPlaca.Should().Be("ABC1D23");
    }

    [Fact]
    public async Task Formato_detalhado_projeta_cliente_veiculo_e_servicos()
    {
        var projecao = ProjecaoCom(2);
        ConfigurarRepo(projecao);

        var handler = new ConsultarAgendaHandler(_repo);
        var resposta = await handler.HandleAsync(
            QueryValida() with { Formato = "detalhado" },
            CancellationToken.None);

        resposta.Data.Should().ContainSingle();
        var item = resposta.Data[0].Should().BeOfType<AgendaItemDetalhadoResponse>().Subject;
        item.Status.Should().Be("AGENDADO");
        item.FilialId.Should().Be(FilialId);
        item.Cliente.Nome.Should().Be("Maria Souza");
        item.Cliente.CpfCnpj.Should().Be("39053344705");
        item.Cliente.Telefone.Should().BeNull();
        item.Cliente.Celular.Should().Be("11987654321");
        item.Veiculo.Placa.Should().Be("ABC1D23");
        item.Servicos.Should().HaveCount(2);
        item.Servicos[0].Nome.Should().Be("Servico 0");
        item.Servicos[0].DuracaoMin.Should().Be(30);
        item.Servicos[0].Preco.Should().Be(50m);
    }

    [Fact]
    public async Task Detalhado_usa_cnpj_quando_nao_ha_cpf()
    {
        var projecao = ProjecaoCom(1) with { ClienteCpf = null, ClienteCnpj = "11222333000181" };
        ConfigurarRepo(projecao);

        var handler = new ConsultarAgendaHandler(_repo);
        var resposta = await handler.HandleAsync(
            QueryValida() with { Formato = "detalhado" },
            CancellationToken.None);

        var item = (AgendaItemDetalhadoResponse)resposta.Data[0];
        item.Cliente.CpfCnpj.Should().Be("11222333000181");
    }

    [Theory]
    [InlineData(0, "Agendamento")]
    [InlineData(1, "Servico 0")]
    [InlineData(2, "Servico 0")]
    [InlineData(3, "Servico 0")]
    public void DerivarTitulo_usa_o_primeiro_servico_ou_fallback(int quantidade, string esperado)
    {
        var servicos = Servicos(quantidade);
        ConsultarAgendaHandler.DerivarTitulo(servicos).Should().Be(esperado);
    }

    [Theory]
    [InlineData(0, "Sem serviços")]
    [InlineData(1, "Servico 0")]
    [InlineData(2, "Servico 0 + 1")]
    [InlineData(3, "Servico 0 + 2")]
    public void DerivarServicosResumo_segue_a_regra_L3(int quantidade, string esperado)
    {
        var servicos = Servicos(quantidade);
        ConsultarAgendaHandler.DerivarServicosResumo(servicos).Should().Be(esperado);
    }

    [Theory]
    [InlineData(0, "Agendamento", "Sem serviços")]
    [InlineData(1, "Servico 0", "Servico 0")]
    [InlineData(2, "Servico 0", "Servico 0 + 1")]
    [InlineData(3, "Servico 0", "Servico 0 + 2")]
    public async Task Simples_deriva_titulo_e_resumo_conforme_quantidade_de_servicos(
        int quantidade,
        string tituloEsperado,
        string resumoEsperado)
    {
        ConfigurarRepo(ProjecaoCom(quantidade));

        var handler = new ConsultarAgendaHandler(_repo);
        var resposta = await handler.HandleAsync(
            QueryValida() with { Formato = "simples" },
            CancellationToken.None);

        var item = (AgendaItemSimplesResponse)resposta.Data[0];
        item.Titulo.Should().Be(tituloEsperado);
        item.ServicosResumo.Should().Be(resumoEsperado);
    }

    [Fact]
    public async Task Status_finalizado_no_banco_serializa_como_concluido()
    {
        ConfigurarRepo(ProjecaoCom(1) with { Status = "finalizado" });

        var handler = new ConsultarAgendaHandler(_repo);
        var resposta = await handler.HandleAsync(
            QueryValida() with { Formato = "simples" },
            CancellationToken.None);

        var item = (AgendaItemSimplesResponse)resposta.Data[0];
        item.Status.Should().Be("CONCLUIDO");
    }

    private void ConfigurarRepo(AgendaProjecao projecao)
    {
        _repo.ConsultarAsync(
                Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AgendaProjecao>)new List<AgendaProjecao> { projecao });
    }

    private static AgendaProjecao ProjecaoCom(int quantidadeServicos) => new()
    {
        AgendamentoId = Guid.NewGuid(),
        Status = "agendado",
        FilialId = FilialId,
        Inicio = Base,
        Fim = Base.AddHours(1),
        DuracaoTotalMin = 60,
        ValorTotal = 100m,
        Observacoes = "Cliente prefere manhã.",
        CriadoEm = Base.AddDays(-1),
        AtualizadoEm = Base.AddDays(-1),
        ClienteId = Guid.NewGuid(),
        ClienteNome = "Maria Souza",
        ClienteCpf = "39053344705",
        ClienteCnpj = null,
        ClienteTelefone = null,
        ClienteCelular = "11987654321",
        VeiculoId = Guid.NewGuid(),
        VeiculoPlaca = "ABC1D23",
        VeiculoModelo = "Civic",
        VeiculoFabricante = "Honda",
        VeiculoCor = "Preto",
        Servicos = Servicos(quantidadeServicos),
    };

    private static IReadOnlyList<AgendaServicoProjecao> Servicos(int quantidade) =>
        Enumerable.Range(0, quantidade)
            .Select(i => new AgendaServicoProjecao
            {
                ItemId = Guid.NewGuid(),
                Id = Guid.NewGuid(),
                Nome = $"Servico {i}",
                DuracaoMin = 30,
                Preco = 50m,
            })
            .ToList();

    private static ConsultarAgendaQuery QueryValida() => new(
        Formato: "simples",
        Inicio: Base.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
        Fim: Base.AddDays(7).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
        FilialId: FilialId.ToString(),
        ClienteId: null,
        UsuarioId: null,
        Status: null,
        TraceId: "trace-1");
}
