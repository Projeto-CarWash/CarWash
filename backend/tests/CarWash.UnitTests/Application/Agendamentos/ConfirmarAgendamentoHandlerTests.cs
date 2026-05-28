using System.Text.Json;
using CarWash.Application.Agendamentos.Abstractions;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Confirmar;
using CarWash.Application.Agendamentos.Persistence;
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

public class ConfirmarAgendamentoHandlerTests
{
    private static readonly Guid FilialId = Guid.NewGuid();
    private static readonly Guid VeiculoId = Guid.NewGuid();
    private static readonly Guid ClienteId = Guid.NewGuid();
    private static readonly Guid ServicoA = Guid.NewGuid();
    private static readonly Guid ServicoB = Guid.NewGuid();
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid IdempotencyKey = Guid.NewGuid();
    private static readonly DateTime Inicio = DateTime.SpecifyKind(
        DateTime.UtcNow.AddDays(1).Date.AddHours(14), DateTimeKind.Utc);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAgendamentoRepository _agendamentos = Substitute.For<IAgendamentoRepository>();
    private readonly IAgendamentoCatalogoRepository _catalogo = Substitute.For<IAgendamentoCatalogoRepository>();
    private readonly IIdempotenciaRepository _idempotencia = Substitute.For<IIdempotenciaRepository>();
    private readonly ITokenConfirmacaoService _tokens;

    public ConfirmarAgendamentoHandlerTests()
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
        _agendamentos.AdicionarComIdempotenciaAsync(
            Arg.Any<Agendamento>(),
            Arg.Any<IReadOnlyCollection<AgendamentoItem>>(),
            Arg.Any<AgendamentoHistorico>(),
            Arg.Any<IdempotenciaRequisicao>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(ResultadoConfirmacaoIdempotente.Persistido());
        _idempotencia.ObterAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotenciaRequisicao?)null);
    }

    [Fact]
    public async Task Caminho_feliz_persiste_e_nao_e_replay()
    {
        var handler = NovoHandler();

        var resultado = await handler.HandleAsync(NovoComando(), CancellationToken.None);

        resultado.EhReplay.Should().BeFalse();
        resultado.Agendamento.Id.Should().NotBeEmpty();
        resultado.Agendamento.Status.Should().Be("agendado");
        resultado.Agendamento.DuracaoTotalMin.Should().Be(75);
        resultado.Agendamento.ValorTotal.Should().Be(75m);

        await _agendamentos.Received(1).AdicionarComIdempotenciaAsync(
            Arg.Is<Agendamento>(a => a.VeiculoId == VeiculoId),
            Arg.Is<IReadOnlyCollection<AgendamentoItem>>(i => i.Count == 2),
            Arg.Any<AgendamentoHistorico>(),
            Arg.Is<IdempotenciaRequisicao>(r => r.IdempotencyKey == IdempotencyKey && r.StatusHttp == 201),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Token_invalido_lanca_TokenConfirmacaoInvalido_400()
    {
        var handler = NovoHandler();
        var act = () => handler.HandleAsync(
            NovoComando() with { TokenConfirmacao = "token.invalido" },
            CancellationToken.None);

        await act.Should().ThrowAsync<TokenConfirmacaoInvalidoException>();
    }

    [Fact]
    public async Task Token_expirado_lanca_SessaoConfirmacaoExpirada_410()
    {
        var handler = NovoHandler();
        var act = () => handler.HandleAsync(
            NovoComando() with { TokenConfirmacao = TokenExpirado() },
            CancellationToken.None);

        await act.Should().ThrowAsync<SessaoConfirmacaoExpiradaException>();
    }

    [Fact]
    public async Task Resumo_divergente_lanca_ResumoDivergente_409()
    {
        // Token assina um hash; o payload da confirmação calcula outro (início diferente).
        var handler = NovoHandler();
        var tokenComOutroHash = TokenValido(hashResumo: new string('f', 64));

        var act = () => handler.HandleAsync(
            NovoComando() with { TokenConfirmacao = tokenComOutroHash },
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ResumoDivergenteException>();
        ex.Which.Slug.Should().Be("agendamento-resumo-divergente");
    }

    [Fact]
    public async Task Confirmacao_com_dados_alterados_apos_a_previa_lanca_ResumoDivergente()
    {
        // O token foi emitido para o início original; a confirmação chega com
        // início deslocado em 1h → hash recalculado difere → 409.
        var handler = NovoHandler();
        var tokenDaPrevia = TokenValido(HashOriginal());

        var act = () => handler.HandleAsync(
            NovoComando() with { TokenConfirmacao = tokenDaPrevia, Inicio = Inicio.AddHours(1) },
            CancellationToken.None);

        await act.Should().ThrowAsync<ResumoDivergenteException>();
    }

    [Fact]
    public async Task Conflito_RN011_na_confirmacao_usa_a_mensagem_do_RF015()
    {
        _agendamentos.ExisteConflitoVeiculoAsync(
            VeiculoId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AgendamentoConflitanteException>();
        ex.Which.Message.Should().Be("O horário não está mais disponível. Atualize e confirme novamente.");
        ex.Which.Slug.Should().Be("agendamento-conflito-veiculo");
    }

    [Fact]
    public async Task Replay_idempotente_com_mesmo_payload_devolve_a_resposta_original()
    {
        var respostaOriginal = new AgendamentoResponse
        {
            Id = Guid.NewGuid(),
            FilialId = FilialId,
            VeiculoId = VeiculoId,
            ClienteId = ClienteId,
            Status = "agendado",
            Mensagem = "Agendamento criado com sucesso.",
        };
        _idempotencia.ObterAsync(IdempotencyKey, ConfirmarAgendamentoHandler.EscopoIdempotencia, Arg.Any<CancellationToken>())
            .Returns(IdempotenciaRequisicao.Registrar(
                id: Guid.NewGuid(),
                idempotencyKey: IdempotencyKey,
                escopo: ConfirmarAgendamentoHandler.EscopoIdempotencia,
                usuarioId: UsuarioId,
                payloadHash: HashOriginal(),
                statusHttp: 201,
                respostaJson: JsonSerializer.Serialize(respostaOriginal, JsonOptions),
                recursoId: respostaOriginal.Id));

        var handler = NovoHandler();
        var resultado = await handler.HandleAsync(
            NovoComando() with { TokenConfirmacao = TokenValido(HashOriginal()) },
            CancellationToken.None);

        resultado.EhReplay.Should().BeTrue();
        resultado.Agendamento.Id.Should().Be(respostaOriginal.Id);

        // Replay não persiste de novo.
        await _agendamentos.DidNotReceive().AdicionarComIdempotenciaAsync(
            Arg.Any<Agendamento>(),
            Arg.Any<IReadOnlyCollection<AgendamentoItem>>(),
            Arg.Any<AgendamentoHistorico>(),
            Arg.Any<IdempotenciaRequisicao>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Idempotencia_com_payload_diferente_lanca_IdempotenciaConflitante_409()
    {
        // Mesma chave já gravada, porém com payload_hash distinto do recalculado.
        _idempotencia.ObterAsync(IdempotencyKey, ConfirmarAgendamentoHandler.EscopoIdempotencia, Arg.Any<CancellationToken>())
            .Returns(IdempotenciaRequisicao.Registrar(
                id: Guid.NewGuid(),
                idempotencyKey: IdempotencyKey,
                escopo: ConfirmarAgendamentoHandler.EscopoIdempotencia,
                usuarioId: UsuarioId,
                payloadHash: new string('b', 64),
                statusHttp: 201,
                respostaJson: "{}",
                recursoId: Guid.NewGuid()));

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(
            NovoComando() with { TokenConfirmacao = TokenValido(HashOriginal()) },
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<IdempotenciaConflitanteException>();
        ex.Which.Slug.Should().Be("idempotencia-conflito");
    }

    [Fact]
    public async Task Replay_resolvido_na_persistencia_por_corrida_concorrente()
    {
        // O lookup inicial não viu o registro, mas a UNIQUE disparou na gravação:
        // o repositório releu o vencedor e devolveu replay.
        var respostaOriginal = new AgendamentoResponse { Id = Guid.NewGuid(), Status = "agendado" };
        _agendamentos.AdicionarComIdempotenciaAsync(
            Arg.Any<Agendamento>(),
            Arg.Any<IReadOnlyCollection<AgendamentoItem>>(),
            Arg.Any<AgendamentoHistorico>(),
            Arg.Any<IdempotenciaRequisicao>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(ResultadoConfirmacaoIdempotente.Replay(
                JsonSerializer.Serialize(respostaOriginal, JsonOptions)));

        var handler = NovoHandler();
        var resultado = await handler.HandleAsync(
            NovoComando() with { TokenConfirmacao = TokenValido(HashOriginal()) },
            CancellationToken.None);

        resultado.EhReplay.Should().BeTrue();
        resultado.Agendamento.Id.Should().Be(respostaOriginal.Id);
    }

    [Fact]
    public async Task Servico_inexistente_lanca_NotFound()
    {
        _catalogo.ObterServicosAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ServicoSnapshot> { new(ServicoA, "Lavagem", 30m, 30, true) });

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Sem_usuario_autenticado_lanca_ValidationException()
    {
        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando() with { UsuarioId = null }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private ConfirmarAgendamentoHandler NovoHandler() =>
        new(
            new CalculadoraResumoAgendamento(_catalogo),
            _agendamentos,
            _idempotencia,
            _tokens,
            NullLogger<ConfirmarAgendamentoHandler>.Instance);

    /// <summary>Hash do payload padrão de <see cref="NovoComando"/>.</summary>
    private static string HashOriginal() =>
        CalculadoraResumoAgendamento.CalcularHashResumo(
            filialId: FilialId,
            clienteId: ClienteId,
            veiculoId: VeiculoId,
            responsavelId: null,
            servicoIds: new[] { ServicoA, ServicoB },
            inicioUtc: Inicio,
            duracaoTotalMin: 75,
            valorTotal: 75m,
            observacoes: "Cliente aguarda.");

    private string TokenValido(string hashResumo) => _tokens.Gerar(hashResumo, UsuarioId, "trace-1");

    private static string TokenExpirado()
    {
        var ontem = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds();
        var payloadJson =
            $"{{\"v\":1,\"hashResumo\":\"{HashOriginal()}\",\"usuarioId\":\"{UsuarioId}\","
            + $"\"traceId\":\"trace-old\",\"iat\":{ontem},\"exp\":{ontem + 900}}}";
        var payloadEncoded = Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(payloadJson));
        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes("chave-de-confirmacao-rf015-com-mais-de-32-bytes-aqui"));
        var assinatura = Base64UrlEncode(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payloadEncoded)));
        return payloadEncoded + "." + assinatura;
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    /// <summary>
    /// Comando padrão já com um <c>tokenConfirmacao</c> válido para o payload —
    /// os testes que exercem token inválido/expirado o sobrescrevem via <c>with</c>.
    /// </summary>
    private ConfirmarAgendamentoCommand NovoComando() => new(
        FilialId: FilialId,
        ClienteId: ClienteId,
        VeiculoId: VeiculoId,
        ResponsavelId: null,
        Inicio: Inicio,
        ServicoIds: new[] { ServicoA, ServicoB },
        Observacoes: "Cliente aguarda.",
        Confirmar: true,
        TokenConfirmacao: TokenValido(HashOriginal()),
        IdempotencyKey: IdempotencyKey,
        TraceId: "trace-1",
        UsuarioId: UsuarioId);
}
