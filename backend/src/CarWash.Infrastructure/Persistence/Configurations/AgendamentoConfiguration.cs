using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public sealed class AgendamentoConfiguration : IEntityTypeConfiguration<Agendamento>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Agendamento> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("agendamentos", t =>
        {
            t.HasCheckConstraint("ck_ag_status", "status IN ('agendado','cancelado','finalizado')");
            t.HasCheckConstraint("ck_ag_inicio_menor_fim", "inicio < fim");
            t.HasCheckConstraint("ck_ag_duracao_total", "duracao_total_min >= 0");
            t.HasCheckConstraint("ck_ag_valor_total", "valor_total >= 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_agendamentos");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.FilialId).IsRequired();
        builder.Property(x => x.ClienteId).IsRequired();
        builder.Property(x => x.VeiculoId).IsRequired();
        builder.Property(x => x.ResponsavelId);
        builder.Property(x => x.CriadoPor).IsRequired();
        builder.Property(x => x.StatusRaw)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("status")
            .HasDefaultValue("agendado");
        builder.Property(x => x.Inicio).IsRequired().HasColumnType("timestamptz");
        builder.Property(x => x.Fim).IsRequired().HasColumnType("timestamptz");
        builder.Property(x => x.Observacoes).HasColumnType("text");
        builder.Property(x => x.DuracaoTotalMin)
            .IsRequired()
            .HasColumnName("duracao_total_min")
            .HasDefaultValue(0);
        builder.Property(x => x.ValorTotal)
            .IsRequired()
            .HasColumnName("valor_total")
            .HasColumnType("numeric(10,2)")
            .HasDefaultValue(0m);
        builder.Property(x => x.Versao)
            .IsRequired()
            .HasDefaultValue(1)
            .IsConcurrencyToken();
        builder.Property(x => x.CriadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(x => x.AtualizadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");

        builder.Ignore(x => x.Status);

        builder.HasOne<Filial>()
            .WithMany()
            .HasForeignKey(x => x.FilialId)
            .HasConstraintName("fk_ag_filial")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(x => x.ClienteId)
            .HasConstraintName("fk_ag_cliente")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Veiculo>()
            .WithMany()
            .HasForeignKey(x => x.VeiculoId)
            .HasConstraintName("fk_ag_veiculo")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Filiado>()
            .WithMany()
            .HasForeignKey(x => x.ResponsavelId)
            .HasConstraintName("fk_ag_responsavel")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.CriadoPor)
            .HasConstraintName("fk_ag_criado_por")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.FilialId, x.Inicio })
            .HasDatabaseName("idx_ag_filial_inicio");

        builder.HasIndex(x => x.StatusRaw).HasDatabaseName("idx_ag_status");

        builder.HasIndex(x => new { x.ClienteId, x.Inicio })
            .HasDatabaseName("idx_ag_cliente")
            .IsDescending(false, true);

        builder.HasIndex(x => new { x.VeiculoId, x.Inicio })
            .HasDatabaseName("idx_ag_veiculo");
    }
}
