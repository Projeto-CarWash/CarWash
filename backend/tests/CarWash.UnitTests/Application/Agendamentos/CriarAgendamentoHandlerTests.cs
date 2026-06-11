using CarWash.Application.Abstractions;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Criar;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Agendamentos;

#pragma warning disable CS0618 // RF007 mantido obsoleto por decisão do ADR 0004 — os testes do caminho legado continuam.
public class CriarAgendamentoHandlerTests
{
    private static readonly Guid FilialId = Guid.NewGuid();
    private static readonly Guid VeiculoId = Guid.NewGuid();
    private static readonly Guid ClienteId = Guid.NewGuid();
    private static readonly Guid ResponsavelId = Guid.NewGuid();
    private static readonly Guid ServicoA = Guid.NewGuid();
    private static readonly Guid ServicoB = Guid.NewGuid();
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private readonly IAgendamentoRepository _agendamentos = Substitute.For<IAgendamentoRepository>();
    private readonly IAgendamentoCatalogoRepository _catalogo = Substitute.For<IAgendamentoCatalogoRepository>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    public CriarAgendamentoHandlerTests()
    {
        _catalogo.ObterFilialResumoAsync(FilialId, Arg.Any<CancellationToken>())
            .Returns(new FilialResumoSnapshot(FilialId, "Filial Centro", true));
        _catalogo.ObterVeiculoResumoAsync(VeiculoId, Arg.Any<CancellationToken>())
            .Returns(new VeiculoResumoSnapshot(VeiculoId, ClienteId, "ABC1D23", "Civic", "Preto", true));
        _catalogo.ObterClienteResumoAsync(ClienteId, Arg.Any<CancellationToken>())
            .Returns(new ClienteResumoSnapshot(ClienteId, "Cliente Teste", "12345678901", true));
        _catalogo.ObterResponsavelResumoAsync(ResponsavelId, Arg.Any<CancellationToken>())
            .Returns(new ResponsavelResumoSnapshot(ResponsavelId, ClienteId, "João", "12345678901", "Irmão", true));
        _catalogo.ObterServicosAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ServicoSnapshot>
            {
                new(ServicoA, "Lavagem Simples", 30m, 30, true),
                new(ServicoB, "Enceramento", 45m, 45, true),
            });

        _agendamentos.ExisteConflitoVeiculoAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _agendamentos.CapacidadeAtingidaAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // RF018/RF008: a CalculadoraResumoAgendamento agora valida capacidade da
        // filial (RN009). Sem stub, o mock retorna celulas_ativas=null/0 e a
        // verificação lançaria CapacidadeFilialEsgotadaException nos caminhos
        // felizes. Capacidade ampla + 0 sobreposições deixa o caminho livre.
        _catalogo.ObterCelulasAtivasFilialAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(50);
        _catalogo.ContarSobreposicoesNaFilialAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0);
    }

    [Fact]
    public async Task Caminho_feliz_calcula_totais_e_persiste()
    {
        var handler = NovoHandler();

        var resposta = await handler.HandleAsync(NovoComando(), CancellationToken.None);

        resposta.Id.Should().NotBeEmpty();
        resposta.ClienteId.Should().Be(ClienteId);
        resposta.DuracaoTotalMin.Should().Be(75);
        resposta.ValorTotal.Should().Be(75m);
        resposta.Status.Should().Be("agendado");
        resposta.Itens.Should().HaveCount(2);
        resposta.Fim.Should().Be(resposta.Inicio.AddMinutes(75));

        await _agendamentos.Received(1).AdicionarAsync(
            Arg.Is<Agendamento>(a => a.DuracaoTotalMin == 75 && a.ValorTotal == 75m && a.VeiculoId == VeiculoId),
            Arg.Is<IReadOnlyCollection<AgendamentoItem>>(itens => itens.Count == 2),
            Arg.Is<AgendamentoHistorico>(h => h.AgendamentoId != Guid.Empty),
            "trace-1",
            ResponsavelId,
            ClienteId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Conflito_RN011_no_precheck_lanca_AgendamentoConflitanteException()
    {
        _agendamentos.ExisteConflitoVeiculoAsync(
            VeiculoId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AgendamentoConflitanteException>();
        ex.Which.Slug.Should().Be("agendamento-conflito-veiculo");

        await _agendamentos.DidNotReceive().AdicionarAsync(
            Arg.Any<Agendamento>(),
            Arg.Any<IReadOnlyCollection<AgendamentoItem>>(),
            Arg.Any<AgendamentoHistorico>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Capacidade_da_filial_atingida_lanca_CapacidadeFilialAtingidaException()
    {
        _agendamentos.CapacidadeAtingidaAsync(
            FilialId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<CapacidadeFilialAtingidaException>();
        ex.Which.Slug.Should().Be("capacidade-filial");

        await _agendamentos.DidNotReceive().AdicionarAsync(
            Arg.Any<Agendamento>(),
            Arg.Any<IReadOnlyCollection<AgendamentoItem>>(),
            Arg.Any<AgendamentoHistorico>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Capacidade_da_filial_abaixo_do_teto_permite_criacao()
    {
        // RF008: CapacidadeAtingidaAsync retorna false → caminho feliz.
        _agendamentos.CapacidadeAtingidaAsync(
            FilialId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(NovoComando(), CancellationToken.None);

        resposta.Id.Should().NotBeEmpty();

        await _agendamentos.Received(1).CapacidadeAtingidaAsync(
            FilialId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Inicio_agora_mesmo_e_aceito()
    {
        // RF008: agendamento para o dia/horário atual é permitido.
        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(
            NovoComando() with { Inicio = DateTime.UtcNow }, CancellationToken.None);

        resposta.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Filial_inexistente_lanca_NotFound_e_audita_motivo_filial_inexistente()
    {
        _catalogo.ObterFilialResumoAsync(FilialId, Arg.Any<CancellationToken>())
            .Returns((FilialResumoSnapshot?)null);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.Message.Should().Be(MensagensFilialAgendamento.NaoEncontrada);

        await _audit.Received(1).LogAsync(
            CalculadoraResumoAgendamento.EventoFilialRejeitada,
            CalculadoraResumoAgendamento.EntidadeAuditoria,
            null,
            Arg.Is<object>(d => MotivoDe(d) == MotivosFalhaFilial.Inexistente),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Filial_inativa_lanca_FilialInativaException()
    {
        _catalogo.ObterFilialResumoAsync(FilialId, Arg.Any<CancellationToken>())
            .Returns(new FilialResumoSnapshot(FilialId, "Filial Centro", false));

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<FilialInativaException>();
        ex.Which.Slug.Should().Be(FilialInativaException.SlugPadrao);
        ex.Which.Message.Should().Be(MensagensFilialAgendamento.Inativa);

        await _audit.Received(1).LogAsync(
            CalculadoraResumoAgendamento.EventoFilialRejeitada,
            CalculadoraResumoAgendamento.EntidadeAuditoria,
            null,
            Arg.Is<object>(d => MotivoDe(d) == MotivosFalhaFilial.Inativa),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Veiculo_inativo_lanca_RecursoInativo()
    {
        _catalogo.ObterVeiculoResumoAsync(VeiculoId, Arg.Any<CancellationToken>())
            .Returns(new VeiculoResumoSnapshot(VeiculoId, ClienteId, "ABC1D23", "Civic", "Preto", false));

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        await act.Should().ThrowAsync<RecursoInativoException>();
    }

    [Fact]
    public async Task Servico_inativo_lanca_RecursoInativo()
    {
        _catalogo.ObterServicosAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ServicoSnapshot>
            {
                new(ServicoA, "Lavagem Simples", 30m, 30, true),
                new(ServicoB, "Enceramento", 45m, 45, false),
            });

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        await act.Should().ThrowAsync<RecursoInativoException>();
    }

    [Fact]
    public async Task Servico_inexistente_lanca_NotFound()
    {
        _catalogo.ObterServicosAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ServicoSnapshot>
            {
                new(ServicoA, "Lavagem Simples", 30m, 30, true),
            });

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Responsavel_de_outro_titular_lanca_ConflictException_CA009_e_audita()
    {
        _catalogo.ObterResponsavelResumoAsync(ResponsavelId, Arg.Any<CancellationToken>())
            .Returns(new ResponsavelResumoSnapshot(ResponsavelId, Guid.NewGuid(), "João", "12345678901", "Irmão", true));

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();

        await _audit.Received(1).LogAsync(
            CalculadoraResumoAgendamento.EventoResponsavelRejeitado,
            CalculadoraResumoAgendamento.EntidadeAuditoria,
            null,
            Arg.Is<object>(d => MotivoDe(d) == MotivosFalhaResponsavel.NaoVinculado),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Responsavel_inativo_lanca_RecursoInativo_e_audita()
    {
        _catalogo.ObterResponsavelResumoAsync(ResponsavelId, Arg.Any<CancellationToken>())
            .Returns(new ResponsavelResumoSnapshot(ResponsavelId, ClienteId, "João", "12345678901", "Irmão", false));

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        await act.Should().ThrowAsync<RecursoInativoException>();

        await _audit.Received(1).LogAsync(
            CalculadoraResumoAgendamento.EventoResponsavelRejeitado,
            CalculadoraResumoAgendamento.EntidadeAuditoria,
            null,
            Arg.Is<object>(d => MotivoDe(d) == MotivosFalhaResponsavel.Inativo),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Responsavel_do_titular_persiste_no_agendamento_CA009()
    {
        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(NovoComando(), CancellationToken.None);

        resposta.ResponsavelId.Should().Be(ResponsavelId);
        await _agendamentos.Received(1).AdicionarAsync(
            Arg.Is<Agendamento>(a => a.ResponsavelId == ResponsavelId),
            Arg.Any<IReadOnlyCollection<AgendamentoItem>>(),
            Arg.Any<AgendamentoHistorico>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sem_usuario_autenticado_lanca_ValidationException()
    {
        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando() with { UsuarioId = null }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Cliente_do_veiculo_inexistente_lanca_NotFound()
    {
        _catalogo.ObterClienteResumoAsync(ClienteId, Arg.Any<CancellationToken>())
            .Returns((ClienteResumoSnapshot?)null);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Cliente_do_veiculo_inativo_lanca_RecursoInativo()
    {
        _catalogo.ObterClienteResumoAsync(ClienteId, Arg.Any<CancellationToken>())
            .Returns(new ClienteResumoSnapshot(ClienteId, "Cliente Teste", "12345678901", false));

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        await act.Should().ThrowAsync<RecursoInativoException>();
    }

    [Fact]
    public async Task Veiculo_de_outro_cliente_lanca_ValidationException_RN002()
    {
        _catalogo.ObterVeiculoResumoAsync(VeiculoId, Arg.Any<CancellationToken>())
            .Returns(new VeiculoResumoSnapshot(VeiculoId, Guid.NewGuid(), "ABC1D23", "Civic", "Preto", true));

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();

        await _agendamentos.DidNotReceive().AdicionarAsync(
            Arg.Any<Agendamento>(),
            Arg.Any<IReadOnlyCollection<AgendamentoItem>>(),
            Arg.Any<AgendamentoHistorico>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Veiculo_inexistente_lanca_NotFound()
    {
        _catalogo.ObterVeiculoResumoAsync(VeiculoId, Arg.Any<CancellationToken>())
            .Returns((VeiculoResumoSnapshot?)null);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Responsavel_inexistente_lanca_NotFound()
    {
        _catalogo.ObterResponsavelResumoAsync(ResponsavelId, Arg.Any<CancellationToken>())
            .Returns((ResponsavelResumoSnapshot?)null);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Um_unico_servico_calcula_fim_corretamente()
    {
        var inicio = DateTime.UtcNow.AddDays(1);
        var handler = NovoHandler();

        var resposta = await handler.HandleAsync(
            NovoComando() with { Inicio = inicio, ServicoIds = new[] { ServicoA } },
            CancellationToken.None);

        resposta.DuracaoTotalMin.Should().Be(30);
        resposta.ValorTotal.Should().Be(30m);
        resposta.Itens.Should().HaveCount(1);
        resposta.Fim.Should().Be(resposta.Inicio.AddMinutes(30));
    }

    [Fact]
    public async Task Servicos_preservam_a_ordem_informada_pelo_cliente()
    {
        var handler = NovoHandler();

        var resposta = await handler.HandleAsync(
            NovoComando() with { ServicoIds = new[] { ServicoB, ServicoA } },
            CancellationToken.None);

        resposta.Itens.Should().HaveCount(2);
        resposta.Itens[0].ServicoId.Should().Be(ServicoB);
        resposta.Itens[1].ServicoId.Should().Be(ServicoA);
    }

    [Fact]
    public async Task Status_inicial_do_agendamento_e_agendado()
    {
        var handler = NovoHandler();

        await handler.HandleAsync(NovoComando(), CancellationToken.None);

        await _agendamentos.Received(1).AdicionarAsync(
            Arg.Is<Agendamento>(a => a.Status == global::CarWash.Domain.Enums.StatusAgendamento.Agendado),
            Arg.Any<IReadOnlyCollection<AgendamentoItem>>(),
            Arg.Any<AgendamentoHistorico>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Persiste_evento_de_historico_CRIADO()
    {
        var handler = NovoHandler();

        await handler.HandleAsync(NovoComando(), CancellationToken.None);

        await _agendamentos.Received(1).AdicionarAsync(
            Arg.Any<Agendamento>(),
            Arg.Any<IReadOnlyCollection<AgendamentoItem>>(),
            Arg.Is<AgendamentoHistorico>(h => h.Evento == global::CarWash.Domain.Enums.EventoHistorico.Criado),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Itens_congelam_preco_e_duracao_do_catalogo()
    {
        var handler = NovoHandler();

        await handler.HandleAsync(
            NovoComando() with { ServicoIds = new[] { ServicoA, ServicoB } },
            CancellationToken.None);

        await _agendamentos.Received(1).AdicionarAsync(
            Arg.Any<Agendamento>(),
            Arg.Is<IReadOnlyCollection<AgendamentoItem>>(itens =>
                itens.Any(i => i.ServicoId == ServicoA && i.PrecoAplicado == 30m && i.DuracaoAplicada == 30)
                && itens.Any(i => i.ServicoId == ServicoB && i.PrecoAplicado == 45m && i.DuracaoAplicada == 45)),
            Arg.Any<AgendamentoHistorico>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Inicio_em_horario_local_e_normalizado_para_UTC()
    {
        var inicioLocal = new DateTime(2099, 6, 1, 14, 0, 0, DateTimeKind.Local);
        var handler = NovoHandler();

        var resposta = await handler.HandleAsync(
            NovoComando() with { Inicio = inicioLocal },
            CancellationToken.None);

        resposta.Inicio.Kind.Should().Be(DateTimeKind.Utc);
        resposta.Fim.Kind.Should().Be(DateTimeKind.Utc);
        resposta.Inicio.Should().Be(inicioLocal.ToUniversalTime());
    }

    [Fact]
    public async Task Pre_check_de_conflito_RN011_usa_a_janela_calculada()
    {
        var inicio = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(1), DateTimeKind.Utc);
        var fimEsperado = inicio.AddMinutes(75);
        var handler = NovoHandler();

        await handler.HandleAsync(
            NovoComando() with { Inicio = inicio, ServicoIds = new[] { ServicoA, ServicoB } },
            CancellationToken.None);

        await _agendamentos.Received(1).ExisteConflitoVeiculoAsync(
            VeiculoId,
            Arg.Is<DateTime>(d => d == inicio),
            Arg.Is<DateTime>(d => d == fimEsperado),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Lê o campo <c>motivo</c> do objeto anônimo <c>dados</c> passado ao
    /// <see cref="IAuditLogger"/> (<c>new { motivo, filialId }</c>). Confirmado
    /// empiricamente: a serialização preserva o nome do membro (camelCase), igual
    /// ao que cai em <c>audit_logs.dados</c>.
    /// </summary>
    private static string? MotivoDe(object dados)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(dados));
        return doc.RootElement.TryGetProperty("motivo", out var motivo)
            ? motivo.GetString()
            : null;
    }

    private CriarAgendamentoHandler NovoHandler() =>
        new(
            _agendamentos,
            new CalculadoraResumoAgendamento(
                _catalogo,
                _audit,
                NullLogger<CalculadoraResumoAgendamento>.Instance),
            NullLogger<CriarAgendamentoHandler>.Instance);

    private static CriarAgendamentoCommand NovoComando() => new(
        FilialId: FilialId,
        ClienteId: ClienteId,
        VeiculoId: VeiculoId,
        ResponsavelId: ResponsavelId,
        Inicio: DateTime.UtcNow.AddDays(1),
        ServicoIds: new[] { ServicoA, ServicoB },
        Observacoes: "Cliente vai aguardar.",
        TraceId: "trace-1",
        UsuarioId: UsuarioId);
}
#pragma warning restore CS0618
