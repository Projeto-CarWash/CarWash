using CarWash.Domain.Common;
using CarWash.Domain.ValueObjects;

namespace CarWash.Domain.Entities;

/// <summary>
/// Veículo do cliente (RF005). Placa única em todo o sistema (RN003) — value object
/// <see cref="Placa"/> normaliza para uppercase sem espaços antes da persistência. (RN003).
/// </summary>
public sealed class Veiculo : IAuditable, IAuditableSetter
{
    public const int AnoMinimo = 1900;
    public const int AnoMaximo = 2100;

    private Veiculo()
    {
        Placa = null!;
        Modelo = null!;
        Fabricante = null!;
        Cor = null!;
    }

    public Guid Id { get; private set; }

    public Guid ClienteId { get; private set; }

    public string Placa { get; private set; }

    public string Modelo { get; private set; }

    public string Fabricante { get; private set; }

    public string Cor { get; private set; }

    public int? Ano { get; private set; }

    public bool Ativo { get; private set; }

    /// <inheritdoc/>
    public DateTime CriadoEm { get; private set; }

    /// <inheritdoc/>
    public DateTime AtualizadoEm { get; private set; }

    public static Veiculo Criar(
        Guid id,
        Guid clienteId,
        Placa placa,
        string modelo,
        string fabricante,
        string cor,
        int? ano = null)
    {
        ArgumentNullException.ThrowIfNull(placa);

        if (id == Guid.Empty)
        {
            throw new DomainException("Id do veículo não pode ser vazio.");
        }

        if (clienteId == Guid.Empty)
        {
            throw new DomainException("Veículo deve estar vinculado a um cliente.");
        }

        if (string.IsNullOrWhiteSpace(modelo) || modelo.Length > 80)
        {
            throw new DomainException("Modelo é obrigatório e deve ter no máximo 80 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(fabricante) || fabricante.Length > 80)
        {
            throw new DomainException("Fabricante é obrigatório e deve ter no máximo 80 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(cor) || cor.Length > 40)
        {
            throw new DomainException("Cor é obrigatória e deve ter no máximo 40 caracteres.");
        }

        if (ano is < AnoMinimo or > AnoMaximo)
        {
            throw new DomainException($"Ano deve estar entre {AnoMinimo} e {AnoMaximo}.");
        }

        var agora = DateTime.UtcNow;
        return new Veiculo
        {
            Id = id,
            ClienteId = clienteId,
            Placa = placa.Valor,
            Modelo = modelo,
            Fabricante = fabricante,
            Cor = cor,
            Ano = ano,
            Ativo = true,
            CriadoEm = agora,
            AtualizadoEm = agora,
        };
    }

    public void Atualizar(
        Placa placa,
        string modelo,
        string fabricante,
        string cor,
        int? ano = null)
    {
        ArgumentNullException.ThrowIfNull(placa);

        if (string.IsNullOrWhiteSpace(modelo) || modelo.Length > 80)
        {
            throw new DomainException("Modelo é obrigatório e deve ter no máximo 80 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(fabricante) || fabricante.Length > 80)
        {
            throw new DomainException("Fabricante é obrigatório e deve ter no máximo 80 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(cor) || cor.Length > 40)
        {
            throw new DomainException("Cor é obrigatória e deve ter no máximo 40 caracteres.");
        }

        if (ano is < AnoMinimo or > AnoMaximo)
        {
            throw new DomainException($"Ano deve estar entre {AnoMinimo} e {AnoMaximo}.");
        }

        Placa = placa.Valor;
        Modelo = modelo;
        Fabricante = fabricante;
        Cor = cor;
        Ano = ano;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void Inativar() => Ativo = false;

    public void Ativar() => Ativo = true;

    /// <inheritdoc/>
    void IAuditableSetter.SetCriadoEm(DateTime valor) => CriadoEm = valor;

    /// <inheritdoc/>
    void IAuditableSetter.SetAtualizadoEm(DateTime valor) => AtualizadoEm = valor;
}
