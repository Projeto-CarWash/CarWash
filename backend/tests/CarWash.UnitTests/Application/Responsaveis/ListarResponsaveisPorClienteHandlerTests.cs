using CarWash.Application.Responsaveis.Listar;
using CarWash.Application.Responsaveis.Persistence;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Responsaveis;

/// <summary>
/// RF023/RF024 — handler de listagem que alimenta o dropdown de responsável.
/// Cobre o caso que originou o bug (GET inexistente): garante o mapeamento
/// entidade→item e a lista vazia.
/// </summary>
public class ListarResponsaveisPorClienteHandlerTests
{
    private readonly IResponsavelRepository _responsaveis = Substitute.For<IResponsavelRepository>();

    private ListarResponsaveisPorClienteHandler NovoHandler() => new(_responsaveis);

    [Fact]
    public async Task Mapeia_responsaveis_para_itens_com_id_nome_documento()
    {
        var clienteId = Guid.NewGuid();
        var r1 = Responsavel.Criar(
            id: Guid.NewGuid(),
            clienteTitularId: clienteId,
            nome: "Ana Responsavel",
            documento: "39053344705",
            grauVinculo: GrauVinculo.ResponsavelFinanceiro);
        var r2 = Responsavel.Criar(
            id: Guid.NewGuid(),
            clienteTitularId: clienteId,
            nome: "Bruno Responsavel",
            documento: "11144477735",
            grauVinculo: GrauVinculo.ResponsavelLegal);

        _responsaveis
            .ListarPorClienteTitularIdAsync(clienteId, Arg.Any<CancellationToken>())
            .Returns(new[] { r1, r2 });

        var resultado = await NovoHandler().HandleAsync(
            new ListarResponsaveisPorClienteQuery(clienteId, "trace-1"),
            CancellationToken.None);

        resultado.Should().HaveCount(2);
        resultado[0].Id.Should().Be(r1.Id);
        resultado[0].Nome.Should().Be("Ana Responsavel");
        resultado[0].Documento.Should().Be("39053344705");
        resultado[0].GrauVinculo.Should().Be(GrauVinculo.ResponsavelFinanceiro.ToDbValue());
        resultado[0].Ativo.Should().BeTrue();
        resultado[1].Id.Should().Be(r2.Id);

        await _responsaveis.Received(1)
            .ListarPorClienteTitularIdAsync(clienteId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sem_responsaveis_retorna_lista_vazia()
    {
        var clienteId = Guid.NewGuid();
        _responsaveis
            .ListarPorClienteTitularIdAsync(clienteId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Responsavel>());

        var resultado = await NovoHandler().HandleAsync(
            new ListarResponsaveisPorClienteQuery(clienteId, "trace-1"),
            CancellationToken.None);

        resultado.Should().BeEmpty();
    }
}
