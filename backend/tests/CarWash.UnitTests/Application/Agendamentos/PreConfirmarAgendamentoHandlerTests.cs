using CarWash.Application.Abstractions;
using CarWash.Application.Agendamentos.Abstractions;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Agendamentos.PreConfirmar;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Entities;
using CarWash.Infrastructure.Agendamentos;
using CarWash.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Agendamentos;

public class PreConfirmarAgendamentoHandlerTests
{
    private static readonly Guid FilialId = Guid.NewGuid();
    private static readonly Guid VeiculoId = Guid.NewGuid();
    private static readonly Guid ClienteId = Guid.NewGuid();
    private static readonly Guid ServicoA = Guid.NewGuid();
    private static readonly Guid ServicoB = Guid.NewGuid();
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private readonly IAgendamentoRepository _agendamentos = Substitute.For<IAgendamentoRepository>();
    private readonly IAgendamentoCatalogoRepository _catalogo = Substitute.For<IAgendamentoCatalogoRepository>();
    private readonly ITokenConfirmacaoService _tokens;

    public PreConfirmarAgendamentoHandlerTests()
    {
        _tokens = new TokenConfirmacaoService(Options.Create(new JwtOptions
        {
            Secret = "secret-do-access-token-distinto-com-mais-de-32-bytes",
            ConfirmacaoSigningKey = "chave-de-confirmacao-rf015-com-mais-de-32-bytes-aqui",
        }));

        _catalogo.ObterFilialResumoAsync(FilialId, Arg.Any<CancellationToken>())
            .Returns(new FilialResumoSnapshot(FilialId, "Filial Centro", true));
        _catalogo.ObterVeiculoResumoAsync(VeiculoId, Arg.Any<CancellationToken>())
            .Returns(new VeiculoResumoSnapshot(VeiculoId, ClienteId, "ABC1D23", "Civic", "Preto", true));
        _catalogo.ObterClienteResumoAsync(ClienteId, Arg.Any<CancellationToken>())
            .Returns(new ClienteResumoSnapshot(ClienteId, "Maria", "12345678901", true));
        _catalogo.ObterServicosAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ServicoSnapshot>
            {
                new(ServicoA, "Lavagem Simples", 30m, 30, true),
                new(ServicoB, "Enceramento", 45m, 45, true),
            });
        _agendamentos.ExisteConflitoVeiculoAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // RF018/RF008: a CalculadoraResumoAgendamento valida capacidade da filial
        // (RN009). Sem stub, o mock retornaria celulas_ativas=null/0 e lançaria
        // CapacidadeFilialEsgotadaException nos caminhos felizes.
        _catalogo.ObterCelulasAtivasFilialAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(50);
        _catalogo.ContarSobreposicoesNaFilialAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0);
    }

    [Fact]
    public async Task Caminho_feliz_monta_resumo_e_token_sem_persistir()
    {
        var handler = NovoHandler();

        var resposta = await handler.HandleAsync(NovoComando(), CancellationToken.None);

        resposta.TokenConfirmacao.Should().NotBeNullOrWhiteSpace();
        resposta.ExpiraEm.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(10));
        resposta.Resumo.Filial.Nome.Should().Be("Filial Centro");
        resposta.Resumo.Cliente.Documento.Should().Be("12345678901");
        resposta.Resumo.Veiculo.Placa.Should().Be("ABC1D23");
        resposta.Resumo.Servicos.Should().HaveCount(2);
        resposta.Resumo.DuracaoTotalMin.Should().Be(75);
        resposta.Resumo.ValorTotal.Should().Be(75m);
        resposta.Resumo.Fim.Should().Be(resposta.Resumo.Inicio.AddMinutes(75));
        resposta.Resumo.HashResumo.Should().HaveLength(64);

        // L9 / RF015: a pré-confirmação não persiste agendamento.
        await _agendamentos.DidNotReceive().AdicionarAsync(
            Arg.Any<Agendamento>(),
            Arg.Any<IReadOnlyCollection<AgendamentoItem>>(),
            Arg.Any<AgendamentoHistorico>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Token_emitido_e_valido_e_carrega_o_hash_do_resumo()
    {
        var handler = NovoHandler();

        var resposta = await handler.HandleAsync(NovoComando(), CancellationToken.None);
        var payload = _tokens.Validar(resposta.TokenConfirmacao, UsuarioId);

        payload.HashResumo.Should().Be(resposta.Resumo.HashResumo);
        payload.UsuarioId.Should().Be(UsuarioId);
    }

    [Fact]
    public async Task Conflito_RN011_na_previa_lanca_AgendamentoConflitante_409()
    {
        // L9: o pré-check de RN011 acontece já na prévia.
        _agendamentos.ExisteConflitoVeiculoAsync(
            VeiculoId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AgendamentoConflitanteException>();
        ex.Which.Slug.Should().Be("agendamento-conflito-veiculo");
    }

    [Fact]
    public async Task Filial_inexistente_lanca_NotFound()
    {
        _catalogo.ObterFilialResumoAsync(FilialId, Arg.Any<CancellationToken>())
            .Returns((FilialResumoSnapshot?)null);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Servico_inativo_lanca_RecursoInativo_422()
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
    public async Task Sem_usuario_autenticado_lanca_ValidationException()
    {
        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando() with { UsuarioId = null }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private PreConfirmarAgendamentoHandler NovoHandler() =>
        new(
            new CalculadoraResumoAgendamento(
                _catalogo,
                Substitute.For<IAuditLogger>(),
                NullLogger<CalculadoraResumoAgendamento>.Instance),
            _agendamentos,
            _tokens,
            NullLogger<PreConfirmarAgendamentoHandler>.Instance);

    private static PreConfirmarAgendamentoCommand NovoComando() => new(
        FilialId: FilialId,
        ClienteId: ClienteId,
        VeiculoId: VeiculoId,
        ResponsavelId: null,
        Inicio: DateTime.UtcNow.AddDays(1),
        ServicoIds: new[] { ServicoA, ServicoB },
        Observacoes: "Cliente aguarda.",
        TraceId: "trace-1",
        UsuarioId: UsuarioId);
}
