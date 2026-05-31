using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public sealed class UsuarioSessaoConfiguration : IEntityTypeConfiguration<UsuarioSessao>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<UsuarioSessao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("usuario_sessoes");

        builder.HasKey(x => x.Id).HasName("pk_usuario_sessoes");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UsuarioId).IsRequired();
        builder.Property(x => x.RefreshTokenHash).IsRequired().HasColumnType("text");
        builder.Property(x => x.ExpiraEm).IsRequired().HasColumnType("timestamptz");
        builder.Property(x => x.RevogadoEm).HasColumnType("timestamptz");
        builder.Property(x => x.IpOrigem).HasMaxLength(45);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.CriadoEm)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .HasConstraintName("fk_sessoes_usuario")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UsuarioId).HasDatabaseName("idx_sessoes_usuario_id");
        builder.HasIndex(x => x.ExpiraEm).HasDatabaseName("idx_sessoes_expira_em");
        builder.HasIndex(x => x.RevogadoEm)
            .HasDatabaseName("idx_sessoes_revogado_em")
            .HasFilter("revogado_em IS NOT NULL");
    }
}
