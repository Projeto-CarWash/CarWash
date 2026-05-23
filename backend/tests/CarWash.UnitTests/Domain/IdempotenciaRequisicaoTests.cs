using CarWash.Domain.Common;
using CarWash.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Domain;

public class IdempotenciaRequisicaoTests
{
    private const string Hash = "abc123abc123abc123abc123abc123abc123abc123abc123abc123abc123abc1";

    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid Key = Guid.NewGuid();
    private static readonly Guid Usuario = Guid.NewGuid();

    [Fact]
    public void Registrar_cria_registro_valido_com_expiracao_de_24h()
    {
        var registro = IdempotenciaRequisicao.Registrar(
            Id, Key, "agendamento-confirmar", Usuario, Hash, 201, "{}", Guid.NewGuid());

        registro.Id.Should().Be(Id);
        registro.IdempotencyKey.Should().Be(Key);
        registro.StatusHttp.Should().Be(201);
        registro.ExpiraEm.Should().BeCloseTo(registro.CriadoEm.AddHours(24), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Registrar_normaliza_o_hash_para_minusculo()
    {
        var registro = IdempotenciaRequisicao.Registrar(
            Id, Key, "agendamento-confirmar", Usuario, Hash.ToUpperInvariant(), 201, "{}");

        registro.PayloadHash.Should().Be(Hash);
    }

    [Fact]
    public void Registrar_com_id_vazio_lanca_DomainException()
    {
        var act = () => IdempotenciaRequisicao.Registrar(
            Guid.Empty, Key, "escopo", Usuario, Hash, 201, "{}");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Registrar_com_chave_vazia_lanca_DomainException()
    {
        var act = () => IdempotenciaRequisicao.Registrar(
            Id, Guid.Empty, "escopo", Usuario, Hash, 201, "{}");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Registrar_com_hash_de_tamanho_invalido_lanca_DomainException()
    {
        var act = () => IdempotenciaRequisicao.Registrar(
            Id, Key, "escopo", Usuario, "hash-curto", 201, "{}");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Registrar_com_status_http_invalido_lanca_DomainException()
    {
        var act = () => IdempotenciaRequisicao.Registrar(
            Id, Key, "escopo", Usuario, Hash, 999, "{}");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Registrar_sem_resposta_lanca_DomainException()
    {
        var act = () => IdempotenciaRequisicao.Registrar(
            Id, Key, "escopo", Usuario, Hash, 201, "  ");

        act.Should().Throw<DomainException>();
    }
}
