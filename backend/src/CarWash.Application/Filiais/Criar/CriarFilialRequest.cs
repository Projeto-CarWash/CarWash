using CarWash.Application.Filiais.Common;

namespace CarWash.Application.Filiais.Criar;

/// <summary>
/// DTO de entrada do <c>POST /api/v1/filiais</c> (RF017). O <c>TraceId</c> e
/// o <c>UsuarioId</c> são preenchidos pelo endpoint — não pertencem ao body.
/// Campo <c>ativo</c> é intencionalmente ausente: a filial nasce sempre ativa
/// (ADR-0007 §2.3 / L4 ratificada).
/// </summary>
public class CriarFilialRequest
{
    public string? Nome { get; set; }

    public string? Codigo { get; set; }

    public string? Cnpj { get; set; }

    public int? CelulasAtivas { get; set; }

    public string? Timezone { get; set; }

    public EnderecoFilialRequest? Endereco { get; set; }
}
