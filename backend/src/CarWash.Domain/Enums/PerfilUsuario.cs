namespace CarWash.Domain.Enums;

/// <summary>
/// Perfil de acesso interno (DAT §6 / DB001 §01). Persistido como string em <c>usuarios.perfil</c>
/// — garantia adicional pelo CHECK <c>ck_usuarios_perfil</c>.
/// </summary>
public enum PerfilUsuario
{
    Admin,
    Funcionario,
}

public static class PerfilUsuarioExtensions
{
    public static string ToDbValue(this PerfilUsuario perfil) => perfil switch
    {
        PerfilUsuario.Admin => "ADMIN",
        PerfilUsuario.Funcionario => "FUNCIONARIO",
        _ => throw new ArgumentOutOfRangeException(nameof(perfil), perfil, "Perfil desconhecido."),
    };

    public static PerfilUsuario FromDbValue(string raw) => raw switch
    {
        "ADMIN" => PerfilUsuario.Admin,
        "FUNCIONARIO" => PerfilUsuario.Funcionario,
        _ => throw new ArgumentOutOfRangeException(nameof(raw), raw, "Perfil persistido inválido."),
    };
}
