using CarWash.Application.Agendamentos.Cancelar;
using FluentValidation;
using FluentValidation.TestHelper;
using Xunit;

namespace CarWash.UnitTests.Application.Agendamentos;

public class CancelarAgendamentoCommandValidatorTests
{
	private readonly CancelarAgendamentoCommandValidator _sut = new();

	[Fact]
	public async Task Comando_valido_passa()
	{
		var command = NovoComando();
		var result = await _sut.TestValidateAsync(command);
		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public async Task AgendamentoId_vazio_falha()
	{
		var command = NovoComando() with { AgendamentoId = Guid.Empty };
		var result = await _sut.TestValidateAsync(command);
		result.ShouldHaveValidationErrorFor(x => x.AgendamentoId);
	}

	[Fact]
	public async Task MotivoCancelamento_vazio_falha()
	{
		var command = NovoComando() with { MotivoCancelamento = string.Empty };
		var result = await _sut.TestValidateAsync(command);
		result.ShouldHaveValidationErrorFor(x => x.MotivoCancelamento);
	}

	[Fact]
	public async Task MotivoCancelamento_menor_que_5_caracteres_falha()
	{
		var command = NovoComando() with { MotivoCancelamento = "abc" };
		var result = await _sut.TestValidateAsync(command);
		result.ShouldHaveValidationErrorFor(x => x.MotivoCancelamento);
	}

	[Fact]
	public async Task MotivoCancelamento_maior_que_500_caracteres_falha()
	{
		var command = NovoComando() with { MotivoCancelamento = new string('x', 501) };
		var result = await _sut.TestValidateAsync(command);
		result.ShouldHaveValidationErrorFor(x => x.MotivoCancelamento);
	}

	[Fact]
	public async Task MotivoCancelamento_exatamente_5_caracteres_passa()
	{
		var command = NovoComando() with { MotivoCancelamento = "abcde" };
		var result = await _sut.TestValidateAsync(command);
		result.ShouldNotHaveValidationErrorFor(x => x.MotivoCancelamento);
	}

	[Fact]
	public async Task Origem_vazia_falha()
	{
		var command = NovoComando() with { Origem = string.Empty };
		var result = await _sut.TestValidateAsync(command);
		result.ShouldHaveValidationErrorFor(x => x.Origem);
	}

	private static CancelarAgendamentoCommand NovoComando() =>
		new(Guid.NewGuid(), "Cliente solicitou cancelamento", "USUARIO_INTERNO", "trace-1", Guid.NewGuid());
}
