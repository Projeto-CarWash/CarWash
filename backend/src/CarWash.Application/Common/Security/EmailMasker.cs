namespace CarWash.Application.Common.Security;

/// <summary>
/// Mascarador de e-mail para logs (ex.: log de tentativa de login). Preserva
/// os 2 primeiros caracteres da parte local + domínio inteiro para diagnóstico,
/// sem expor a identificação completa do usuário.
///
/// <para>Exemplo: <c>guilherme@empresa.com</c> → <c>gu***@empresa.com</c>.</para>
///
/// <para>
/// Centralizado em <c>Application/Common/Security</c> para que tanto handlers
/// (Application) quanto loggers de auditoria (Infrastructure) reutilizem a
/// mesma política — evita drift entre dois caminhos de mascaramento.
/// </para>
/// </summary>
public static class EmailMasker
{
    /// <summary>
    /// Mascara o e-mail informado. Retorna <c>***@***</c> para entrada vazia
    /// ou inválida (não-2-partes ou parte local com ≤ 2 chars).
    /// </summary>
    /// <returns></returns>
    public static string Mask(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "***@***";
        }

        string[] partes = email.Split('@');
        if (partes.Length != 2 || partes[0].Length <= 2 || string.IsNullOrEmpty(partes[1]))
        {
            return "***@***";
        }

        return string.Concat(partes[0].AsSpan(0, 2), "***@", partes[1]);
    }
}
