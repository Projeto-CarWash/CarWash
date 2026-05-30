using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public sealed class AgendamentoObservacaoConfiguration : IEntityTypeConfiguration<AgendamentoObservacao>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<AgendamentoObservacao> builder)
    {
        builder.ToTable("agendamento_observacoes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.AgendamentoId)
            .HasColumnName("agendamento_id")
            .IsRequired();

        builder.Property(x => x.Texto)
            .HasColumnName("texto")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Ativo)
            .HasColumnName("ativo")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CriadoPor)
            .HasColumnName("criado_por")
            .IsRequired();

        builder.Property(x => x.AtualizadoEm)
            .HasColumnName("atualizado_em")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.AtualizadoPor)
            .HasColumnName("atualizado_por");

        builder.Property(x => x.ExcluidoEm)
            .HasColumnName("excluido_em")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ExcluidoPor)
            .HasColumnName("excluido_por");

        builder.HasIndex(x => x.AgendamentoId)
            .HasDatabaseName("idx_obs_agendamento_id");

        builder.HasIndex(x => new { x.AgendamentoId, x.Ativo })
            .HasDatabaseName("idx_obs_agendamento_ativo");
    }
}
