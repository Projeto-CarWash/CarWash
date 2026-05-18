using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public sealed class UsuarioPreferenciaConfiguration : IEntityTypeConfiguration<UsuarioPreferencia>
{
    public void Configure(EntityTypeBuilder<UsuarioPreferencia> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("usuario_preferencias", t =>
            t.HasCheckConstraint("ck_pref_tema", "tema IN ('claro','escuro')"));

        builder.HasKey(x => x.Id).HasName("pk_usuario_preferencias");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UsuarioId).IsRequired();
        builder.Property(x => x.TemaRaw)
            .IsRequired()
            .HasMaxLength(10)
            .HasColumnName("tema")
            .HasDefaultValue("claro");
        builder.Property(x => x.AtualizadoEm)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.Ignore(x => x.Tema);

        builder.HasOne<Usuario>()
            .WithOne()
            .HasForeignKey<UsuarioPreferencia>(x => x.UsuarioId)
            .HasConstraintName("fk_pref_usuario")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UsuarioId)
            .IsUnique()
            .HasDatabaseName("uk_pref_usuario_id");
    }
}
