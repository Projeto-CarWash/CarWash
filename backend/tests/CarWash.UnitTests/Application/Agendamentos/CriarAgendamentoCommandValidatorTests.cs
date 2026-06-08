using CarWash.Application.Agendamentos.Criar;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Xunit;

namespace CarWash.UnitTests.Application.Agendamentos;

public class CriarAgendamentoCommandValidatorTests
{
    private readonly IValidator<CriarAgendamentoCommand> _sut = new CriarAgendamentoCommandValidator();

    [Fact]
    public async Task Command_valido_passa()
    {
        var resultado = await _sut.ValidateAsync(CommandValido(), CancellationToken.None);
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task FilialId_vazio_falha()
    {
        var cmd = CommandValido() with { FilialId = Guid.Empty };
        var resultado = await _sut.ValidateAsync(cmd, CancellationToken.None);
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle(e => e.PropertyName == "FilialId");
    }

    [Fact]
    public async Task ClienteId_vazio_falha()
    {
        var cmd = CommandValido() with { ClienteId = Guid.Empty };
        var resultado = await _sut.ValidateAsync(cmd, CancellationToken.None);
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle(e => e.PropertyName == "ClienteId");
    }

    [Fact]
    public async Task VeiculoId_vazio_falha()
    {
        var cmd = CommandValido() with { VeiculoId = Guid.Empty };
        var resultado = await _sut.ValidateAsync(cmd, CancellationToken.None);
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle(e => e.PropertyName == "VeiculoId");
    }

    [Fact]
    public async Task Inicio_default_falha()
    {
        var cmd = CommandValido() with { Inicio = default };
        var resultado = await _sut.ValidateAsync(cmd, CancellationToken.None);
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Inicio");
    }

    [Fact]
    public async Task Inicio_no_passado_falha()
    {
        var cmd = CommandValido() with { Inicio = DateTime.UtcNow.AddHours(-1) };
        var resultado = await _sut.ValidateAsync(cmd, CancellationToken.None);
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Inicio");
    }

    [Fact]
    public async Task Inicio_agora_passa()
    {
        var cmd = CommandValido() with { Inicio = DateTime.UtcNow };
        var resultado = await _sut.ValidateAsync(cmd, CancellationToken.None);
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ServicoIds_vazio_falha()
    {
        var cmd = CommandValido() with { ServicoIds = [] };
        var resultado = await _sut.ValidateAsync(cmd, CancellationToken.None);
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ServicoIds_duplicado_falha()
    {
        var id = Guid.NewGuid();
        var cmd = CommandValido() with { ServicoIds = [id, id] };
        var resultado = await _sut.ValidateAsync(cmd, CancellationToken.None);
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ServicoId_vazio_falha()
    {
        var cmd = CommandValido() with { ServicoIds = [Guid.NewGuid(), Guid.Empty] };
        var resultado = await _sut.ValidateAsync(cmd, CancellationToken.None);
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Observacoes_maior_que_1000_falha()
    {
        var cmd = CommandValido() with { Observacoes = new string('a', 1001) };
        var resultado = await _sut.ValidateAsync(cmd, CancellationToken.None);
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ResponsavelId_vazio_falha()
    {
        var cmd = CommandValido() with { ResponsavelId = Guid.Empty };
        var resultado = await _sut.ValidateAsync(cmd, CancellationToken.None);
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle(e => e.PropertyName == "ResponsavelId");
    }

    [Fact]
    public async Task ResponsavelId_nulo_passa()
    {
        var cmd = CommandValido() with { ResponsavelId = null };
        var resultado = await _sut.ValidateAsync(cmd, CancellationToken.None);
        resultado.IsValid.Should().BeTrue();
    }

    private static CriarAgendamentoCommand CommandValido() => new(
        FilialId: Guid.NewGuid(),
        ClienteId: Guid.NewGuid(),
        VeiculoId: Guid.NewGuid(),
        ResponsavelId: null,
        Inicio: DateTime.UtcNow.AddHours(1),
        ServicoIds: [Guid.NewGuid()],
        Observacoes: null,
        TraceId: "trace-test",
        UsuarioId: Guid.NewGuid());
}
