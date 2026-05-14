using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Evento)
            .HasColumnName("evento")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.Entidade)
            .HasColumnName("entidade")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.EntidadeId)
            .HasColumnName("entidade_id");

        builder.Property(x => x.UsuarioId)
            .HasColumnName("usuario_id");

        builder.Property(x => x.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Dados)
            .HasColumnName("dados")
            .HasColumnType("jsonb");

        builder.Property(x => x.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.Evento)
            .HasDatabaseName("idx_audit_evento");

        builder.HasIndex(x => x.Entidade)
            .HasDatabaseName("idx_audit_entidade");

        builder.HasIndex(x => x.CriadoEm)
            .HasDatabaseName("idx_audit_criado_em");

        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("idx_audit_correlation");
    }
}
