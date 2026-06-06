using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento da tabela <c>idempotencia_requisicoes</c> (RF015). A constraint
/// UNIQUE <c>uq_idempotencia_key_escopo</c> é a defesa anti-race da confirmação
/// idempotente; o índice em <c>expira_em</c> apoia o job de limpeza.
/// </summary>
public sealed class IdempotenciaRequisicaoConfiguration : IEntityTypeConfiguration<IdempotenciaRequisicao>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<IdempotenciaRequisicao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("idempotencia_requisicoes");

        builder.HasKey(x => x.Id).HasName("pk_idempotencia_requisicoes");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.IdempotencyKey).IsRequired();
        builder.Property(x => x.Escopo).IsRequired().HasMaxLength(80);
        builder.Property(x => x.UsuarioId).IsRequired();
        builder.Property(x => x.PayloadHash)
            .IsRequired()
            .HasColumnType("char(64)")
            .IsFixedLength();
        builder.Property(x => x.StatusHttp).IsRequired();
        builder.Property(x => x.RespostaJson).IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.RecursoId);
        builder.Property(x => x.CriadoEm)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
        builder.Property(x => x.ExpiraEm).IsRequired().HasColumnType("timestamptz");

        // Coluna de auditoria (DB001 §07) — não exposta como propriedade pública distinta
        // do contrato; mantida para uniformidade com as demais entidades IAuditable.
        builder.Property(x => x.AtualizadoEm)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.HasIndex(x => new { x.IdempotencyKey, x.Escopo })
            .IsUnique()
            .HasDatabaseName("uq_idempotencia_key_escopo");

        builder.HasIndex(x => x.ExpiraEm)
            .HasDatabaseName("ix_idempotencia_expira_em");
    }
}
