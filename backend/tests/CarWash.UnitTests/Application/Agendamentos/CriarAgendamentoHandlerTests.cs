using CarWash.Application.Abstractions;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Criar;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Agendamentos;

public class CriarAgendamentoHandlerTests
{
    private readonly IAgendamentoRepository _repo = Substitute.For<IAgendamentoRepository>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();
    private readonly ILogger<CriarAgendamentoHandler> _logger = Substitute.For<ILogger<CriarAgendamentoHandler>>();

    private readonly Guid _filialId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private readonly Guid _clienteId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private readonly Guid _veiculoId = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private readonly Guid _servicoId = Guid.Parse("40000000-0000-0000-0000-000000000004");
    private readonly Guid _usuarioId = Guid.Parse("50000000-0000-0000-0000-000000000005");

    [Fact]
    public async Task Caminho_feliz_retorna_response_com_dados()
    {
        SetupEntidadesValidas();

        var handler = NovoHandler();
        var cmd = CommandValido();

        var resposta = await handler.HandleAsync(cmd, CancellationToken.None);

        resposta.Message.Should().Be("Agendamento criado com sucesso.");
        resposta.Data.FilialId.Should().Be(_filialId);
        resposta.Data.ClienteId.Should().Be(_clienteId);
        resposta.Data.VeiculoId.Should().Be(_veiculoId);
        resposta.Data.Status.Should().Be("AGENDADO");
        resposta.Data.DuracaoTotalMin.Should().Be(30);
        resposta.Data.ValorTotal.Should().Be(50m);
        resposta.TraceId.Should().Be("trace-1");

        await _repo.Received(1).CriarAsync(
            Arg.Any<Agendamento>(),
            Arg.Any<List<AgendamentoItem>>(),
            Arg.Any<AgendamentoHistorico>(),
            "trace-1",
            _usuarioId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Filial_nao_encontrada_lanca_NotFoundException()
    {
        _repo.ObterFilialPorIdAsync(_filialId, Arg.Any<CancellationToken>())
            .Returns((Filial?)null);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(CommandValido(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Filial*");
    }

    [Fact]
    public async Task Filial_inativa_lanca_NotFoundException()
    {
        var filial = FilialAtiva();
        filial.Inativar();
        _repo.ObterFilialPorIdAsync(_filialId, Arg.Any<CancellationToken>()).Returns(filial);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(CommandValido(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Filial*");
    }

    [Fact]
    public async Task Cliente_nao_encontrado_lanca_NotFoundException()
    {
        SetupFilialAtiva();
        _repo.ObterClientePorIdAsync(_clienteId, Arg.Any<CancellationToken>())
            .Returns((Cliente?)null);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(CommandValido(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Cliente*");
    }

    [Fact]
    public async Task Veiculo_nao_encontrado_lanca_NotFoundException()
    {
        SetupFilialAtiva();
        SetupClienteAtivo();
        _repo.ObterVeiculoPorIdAsync(_veiculoId, Arg.Any<CancellationToken>())
            .Returns((Veiculo?)null);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(CommandValido(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Veículo*");
    }

    [Fact]
    public async Task Veiculo_de_outro_cliente_lanca_ValidationException()
    {
        SetupFilialAtiva();
        SetupClienteAtivo();

        var veiculoOutroCliente = VeiculoAtivo(Guid.Parse("90000000-0000-0000-0000-000000000009"));
        _repo.ObterVeiculoPorIdAsync(_veiculoId, Arg.Any<CancellationToken>())
            .Returns(veiculoOutroCliente);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(CommandValido(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Erros.Should().ContainKey("veiculoId");
    }

    [Fact]
    public async Task Servico_nao_encontrado_lanca_ValidationException()
    {
        SetupFilialAtiva();
        SetupClienteAtivo();
        SetupVeiculoAtivo();
        _repo.ObterServicosPorIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Servico>());

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(CommandValido(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Erros.Should().ContainKey("servicoIds");
    }

    [Fact]
    public async Task Servico_inativo_lanca_ValidationException()
    {
        SetupFilialAtiva();
        SetupClienteAtivo();
        SetupVeiculoAtivo();

        var servico = ServicoAtivo();
        servico.Inativar();
        _repo.ObterServicosPorIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Servico> { servico });

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(CommandValido(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Erros.Should().ContainKey("servicoIds");
    }

    [Fact]
    public async Task Conflito_veiculo_lanca_VeiculoConflitoException_e_loga_auditoria()
    {
        SetupEntidadesValidas();
        _repo.ExisteConflitoVeiculoAsync(_veiculoId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(CommandValido(), CancellationToken.None);

        await act.Should().ThrowAsync<VeiculoConflitoException>();
        await _auditLogger.Received(1).LogAsync(
            "AGENDAMENTO_REJEITADO",
            "agendamentos",
            null,
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Capacidade_atingida_lanca_CapacidadeFilialAtingidaException_e_loga_auditoria()
    {
        SetupEntidadesValidas();
        _repo.ExisteConflitoVeiculoAsync(_veiculoId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _repo.ContarOcupacaoAsync(_filialId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(4);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(CommandValido(), CancellationToken.None);

        await act.Should().ThrowAsync<CapacidadeFilialAtingidaException>();
        await _auditLogger.Received(1).LogAsync(
            "AGENDAMENTO_REJEITADO",
            "agendamentos",
            null,
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Capacidade_nao_atingida_sucesso()
    {
        SetupEntidadesValidas();
        _repo.ExisteConflitoVeiculoAsync(_veiculoId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _repo.ContarOcupacaoAsync(_filialId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(3);

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(CommandValido(), CancellationToken.None);

        resposta.Message.Should().Be("Agendamento criado com sucesso.");
    }

    [Fact]
    public async Task Command_null_lanca_ArgumentNullException()
    {
        var handler = NovoHandler();
        var act = () => handler.HandleAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private CriarAgendamentoHandler NovoHandler() => new(_repo, _auditLogger, _logger);

    private CriarAgendamentoCommand CommandValido() => new(
        FilialId: _filialId,
        ClienteId: _clienteId,
        VeiculoId: _veiculoId,
        Inicio: DateTime.UtcNow.AddHours(1),
        ServicoIds: [_servicoId],
        Observacoes: null,
        TraceId: "trace-1",
        UsuarioId: _usuarioId);

    private void SetupEntidadesValidas()
    {
        SetupFilialAtiva();
        SetupClienteAtivo();
        SetupVeiculoAtivo();
        SetupServicosAtivos();
        _repo.ExisteConflitoVeiculoAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _repo.ContarOcupacaoAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0);
    }

    private void SetupFilialAtiva()
    {
        _repo.ObterFilialPorIdAsync(_filialId, Arg.Any<CancellationToken>())
            .Returns(FilialAtiva());
    }

    private void SetupClienteAtivo()
    {
        _repo.ObterClientePorIdAsync(_clienteId, Arg.Any<CancellationToken>())
            .Returns(ClienteAtivo());
    }

    private void SetupVeiculoAtivo()
    {
        _repo.ObterVeiculoPorIdAsync(_veiculoId, Arg.Any<CancellationToken>())
            .Returns(VeiculoAtivo(_clienteId));
    }

    private void SetupServicosAtivos()
    {
        _repo.ObterServicosPorIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Servico> { ServicoAtivo() });
    }

    private Filial FilialAtiva() => Filial.Criar(
        id: _filialId,
        nome: "Matriz Teste",
        celulasAtivas: 4);

    private Cliente ClienteAtivo() => Cliente.Criar(
        id: _clienteId,
        nome: "João Silva",
        dataNascimento: new DateOnly(1990, 1, 1),
        celular: new Telefone("11999999999"),
        endereco: new Endereco(
            "12345678", "Rua Teste", "100", null, "Centro", "São Paulo", "SP"),
        cpf: new Cpf("52998224725"));

    private Veiculo VeiculoAtivo(Guid clienteId) => Veiculo.Criar(
        id: _veiculoId,
        clienteId: clienteId,
        placa: new Placa("ABC1D23"),
        modelo: "Civic",
        fabricante: "Honda",
        cor: "Preto");

    private Servico ServicoAtivo() => Servico.Criar(
        id: _servicoId,
        nome: "Lavagem Simples",
        preco: 50m,
        duracaoMin: 30);
}
