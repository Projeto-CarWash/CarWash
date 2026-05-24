using System.Globalization;
using CarWash.Application.Agenda.Consultar;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Application.Agenda;

public class ConsultarAgendaQueryValidatorTests
{
    private static readonly DateTime Base = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly ConsultarAgendaQueryValidator _validator = new();

    [Fact]
    public void Query_valida_passa()
    {
        var resultado = _validator.Validate(QueryValida());
        resultado.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("simples")]
    [InlineData("detalhado")]
    [InlineData("SIMPLES")]
    [InlineData("Detalhado")]
    public void Formato_valido_em_qualquer_caixa_passa(string formato)
    {
        var resultado = _validator.Validate(QueryValida() with { Formato = formato });
        resultado.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("resumido")]
    public void Formato_invalido_ou_ausente_falha(string? formato)
    {
        var resultado = _validator.Validate(QueryValida() with { Formato = formato });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ConsultarAgendaQuery.Formato)
            && e.ErrorMessage == "Formato é obrigatório e deve ser 'simples' ou 'detalhado'.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ontem")]
    [InlineData("2026-13-01T00:00:00Z")]
    public void Inicio_ausente_ou_nao_parseavel_falha(string? inicio)
    {
        var resultado = _validator.Validate(QueryValida() with { Inicio = inicio });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ConsultarAgendaQuery.Inicio)
            && e.ErrorMessage == "Início é obrigatório e deve estar em formato ISO-8601 UTC.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("amanha")]
    public void Fim_ausente_ou_nao_parseavel_falha(string? fim)
    {
        var resultado = _validator.Validate(QueryValida() with { Fim = fim });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ConsultarAgendaQuery.Fim)
            && e.ErrorMessage == "Fim é obrigatório e deve estar em formato ISO-8601 UTC.");
    }

    [Fact]
    public void Inicio_igual_ao_fim_falha()
    {
        var instante = Iso(Base);
        var resultado = _validator.Validate(QueryValida() with { Inicio = instante, Fim = instante });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorMessage == "O início deve ser anterior ao fim.");
    }

    [Fact]
    public void Inicio_posterior_ao_fim_falha()
    {
        var resultado = _validator.Validate(QueryValida() with
        {
            Inicio = Iso(Base.AddDays(2)),
            Fim = Iso(Base.AddDays(1)),
        });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorMessage == "O início deve ser anterior ao fim.");
    }

    [Fact]
    public void Janela_de_exatamente_31_dias_passa()
    {
        // Boundary: 31 dias é o limite — valor no limite deve ser aceito.
        var resultado = _validator.Validate(QueryValida() with
        {
            Inicio = Iso(Base),
            Fim = Iso(Base.AddDays(31)),
        });

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Janela_de_31_dias_e_um_segundo_falha()
    {
        var resultado = _validator.Validate(QueryValida() with
        {
            Inicio = Iso(Base),
            Fim = Iso(Base.AddDays(31).AddSeconds(1)),
        });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.ErrorMessage == "A janela de consulta não pode exceder 31 dias.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nao-e-guid")]
    public void FilialId_ausente_ou_malformado_falha(string? filialId)
    {
        var resultado = _validator.Validate(QueryValida() with { FilialId = filialId });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ConsultarAgendaQuery.FilialId)
            && e.ErrorMessage == "Filial é obrigatória e deve ser um identificador válido.");
    }

    [Fact]
    public void ClienteId_ausente_passa_por_ser_opcional()
    {
        var resultado = _validator.Validate(QueryValida() with { ClienteId = null });
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ClienteId_presente_e_malformado_falha()
    {
        var resultado = _validator.Validate(QueryValida() with { ClienteId = "abc" });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ConsultarAgendaQuery.ClienteId)
            && e.ErrorMessage == "Cliente informado é inválido.");
    }

    [Fact]
    public void UsuarioId_ausente_passa_por_ser_opcional()
    {
        var resultado = _validator.Validate(QueryValida() with { UsuarioId = null });
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UsuarioId_presente_e_malformado_falha()
    {
        var resultado = _validator.Validate(QueryValida() with { UsuarioId = "xyz" });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ConsultarAgendaQuery.UsuarioId)
            && e.ErrorMessage == "Responsável informado é inválido.");
    }

    [Fact]
    public void Status_ausente_passa_por_ser_opcional()
    {
        var resultado = _validator.Validate(QueryValida() with { Status = null });
        resultado.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("AGENDADO")]
    [InlineData("EM_ANDAMENTO")]
    [InlineData("CONCLUIDO")]
    [InlineData("CANCELADO")]
    [InlineData("agendado")]
    public void Status_dentro_do_contrato_passa(string status)
    {
        // EM_ANDAMENTO é filtro válido (ADR 0004 — L1): NÃO retorna 400.
        var resultado = _validator.Validate(QueryValida() with { Status = status });
        resultado.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("FINALIZADO")]
    [InlineData("PENDENTE")]
    [InlineData("123")]
    public void Status_fora_do_contrato_falha(string status)
    {
        var resultado = _validator.Validate(QueryValida() with { Status = status });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ConsultarAgendaQuery.Status)
            && e.ErrorMessage == "Status informado é inválido.");
    }

    private static ConsultarAgendaQuery QueryValida() => new(
        Formato: "simples",
        Inicio: Iso(Base),
        Fim: Iso(Base.AddDays(7)),
        FilialId: Guid.NewGuid().ToString(),
        ClienteId: null,
        UsuarioId: null,
        Status: null,
        TraceId: "trace-1");

    private static string Iso(DateTime utc) =>
        utc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
}
