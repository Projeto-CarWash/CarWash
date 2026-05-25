using CarWash.Domain.Common;

namespace CarWash.Domain.Entities;

/// <summary>
/// Registro de idempotência de uma requisição de escrita (RF015 — confirmação de
/// agendamento). Garante que duplo clique e retry de rede produzam um único efeito:
/// a chave <c>idempotency_key</c> + <c>escopo</c> é única; o <c>payload_hash</c>
/// distingue replay legítimo (mesmo payload → devolve a resposta gravada) de uso
/// indevido da chave (payload diferente → conflito). Expira em 24h; a limpeza é
/// feita por um job de varredura. Id gerado pela aplicação (ADR 0001).
/// </summary>
public sealed class IdempotenciaRequisicao : IAuditable, IAuditableSetter
{
    /// <summary>Janela de validade de um registro de idempotência (RF015).</summary>
    public static readonly TimeSpan JanelaValidade = TimeSpan.FromHours(24);

    private IdempotenciaRequisicao()
    {
        Escopo = null!;
        PayloadHash = null!;
        RespostaJson = null!;
    }

    public Guid Id { get; private set; }

    /// <summary>Chave de idempotência informada pelo cliente.</summary>
    public Guid IdempotencyKey { get; private set; }

    /// <summary>Escopo lógico da operação — segmenta a chave por caso de uso.</summary>
    public string Escopo { get; private set; }

    /// <summary>Usuário autenticado que originou a requisição.</summary>
    public Guid UsuarioId { get; private set; }

    /// <summary>SHA-256 (hex) do payload de negócio — o mesmo <c>hashResumo</c> da confirmação.</summary>
    public string PayloadHash { get; private set; }

    /// <summary>Status HTTP da resposta original gravada.</summary>
    public int StatusHttp { get; private set; }

    /// <summary>Corpo serializado da resposta original — devolvido no replay.</summary>
    public string RespostaJson { get; private set; }

    /// <summary>Id do recurso criado pela requisição original (quando aplicável).</summary>
    public Guid? RecursoId { get; private set; }

    public DateTime CriadoEm { get; private set; }

    /// <summary>Momento a partir do qual o registro pode ser descartado pelo job de limpeza.</summary>
    public DateTime ExpiraEm { get; private set; }

    /// <summary>
    /// <see cref="IAuditable.AtualizadoEm"/> — o registro é imutável após criado;
    /// mantido por convenção de auditoria (DB001 §07).
    /// </summary>
    public DateTime AtualizadoEm { get; private set; }

    /// <summary>
    /// Cria um registro de idempotência já com a resposta original capturada. A
    /// expiração é derivada de <see cref="JanelaValidade"/> a partir de agora (UTC).
    /// </summary>
    public static IdempotenciaRequisicao Registrar(
        Guid id,
        Guid idempotencyKey,
        string escopo,
        Guid usuarioId,
        string payloadHash,
        int statusHttp,
        string respostaJson,
        Guid? recursoId = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Id do registro de idempotência não pode ser vazio.");
        }

        if (idempotencyKey == Guid.Empty)
        {
            throw new DomainException("Chave de idempotência não pode ser vazia.");
        }

        if (string.IsNullOrWhiteSpace(escopo) || escopo.Length > 80)
        {
            throw new DomainException("Escopo de idempotência é obrigatório e deve ter no máximo 80 caracteres.");
        }

        if (usuarioId == Guid.Empty)
        {
            throw new DomainException("Registro de idempotência exige usuário.");
        }

        if (string.IsNullOrWhiteSpace(payloadHash) || payloadHash.Length != 64)
        {
            throw new DomainException("Hash do payload deve ser um SHA-256 hexadecimal de 64 caracteres.");
        }

        if (statusHttp is < 100 or > 599)
        {
            throw new DomainException("Status HTTP do registro de idempotência é inválido.");
        }

        if (string.IsNullOrWhiteSpace(respostaJson))
        {
            throw new DomainException("Resposta gravada do registro de idempotência é obrigatória.");
        }

        var agora = DateTime.UtcNow;
        return new IdempotenciaRequisicao
        {
            Id = id,
            IdempotencyKey = idempotencyKey,
            Escopo = escopo,
            UsuarioId = usuarioId,
            PayloadHash = payloadHash.ToLowerInvariant(),
            StatusHttp = statusHttp,
            RespostaJson = respostaJson,
            RecursoId = recursoId,
            CriadoEm = agora,
            ExpiraEm = agora.Add(JanelaValidade),
        };
    }

    void IAuditableSetter.SetCriadoEm(DateTime valor) => CriadoEm = valor;

    void IAuditableSetter.SetAtualizadoEm(DateTime valor) => AtualizadoEm = valor;
}
