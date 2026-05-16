using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento da entidade <see cref="Usuario"/> → tabela <c>usuarios</c>.
/// </summary>
public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("usuarios", t =>
            t.HasCheckConstraint(
                "ck_usuarios_perfil",
                "perfil IN ('ADMIN','FUNCIONARIO')"));

        builder.HasKey(x => x.Id).HasName("pk_usuarios");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Nome).IsRequired().HasMaxLength(120);
        builder.Property(x => x.EmailValor).IsRequired().HasMaxLength(150).HasColumnName("email");
        builder.Property(x => x.SenhaHash).IsRequired().HasColumnType("text");
        builder.Property(x => x.PerfilRaw).IsRequired().HasMaxLength(20).HasColumnName("perfil");

        builder.Property(x => x.Ativo).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CriadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(x => x.AtualizadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");

        builder.Ignore(x => x.Email);
        builder.Ignore(x => x.Perfil);

        builder.HasIndex(x => x.EmailValor)
            .IsUnique()
            .HasDatabaseName("uk_usuarios_email");

        builder.HasIndex(x => x.Ativo)
            .HasDatabaseName("idx_usuarios_ativo")
            .HasFilter("ativo = false");
    }
}
