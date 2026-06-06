using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id).HasName("pk_audit_logs");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Evento).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Entidade).IsRequired().HasMaxLength(80);
        builder.Property(x => x.EntidadeId);
        builder.Property(x => x.UsuarioId);
        builder.Property(x => x.CorrelationId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Dados).HasColumnType("jsonb");
        builder.Property(x => x.CriadoEm)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .HasConstraintName("fk_audit_usuario")
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(x => new { x.Evento, x.CriadoEm })
            .HasDatabaseName("idx_audit_evento")
            .IsDescending(false, true);
        builder.HasIndex(x => new { x.Entidade, x.EntidadeId })
            .HasDatabaseName("idx_audit_entidade");
        builder.HasIndex(x => x.CriadoEm)
            .HasDatabaseName("idx_audit_criado_em")
            .IsDescending();
        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("idx_audit_correlation");
    }
}
