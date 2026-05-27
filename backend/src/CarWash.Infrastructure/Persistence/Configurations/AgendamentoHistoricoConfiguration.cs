using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public sealed class AgendamentoHistoricoConfiguration : IEntityTypeConfiguration<AgendamentoHistorico>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<AgendamentoHistorico> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("agendamento_historico", t =>
            t.HasCheckConstraint(
                "ck_hist_evento",
                "evento IN ('CRIADO','EDITADO','CANCELADO','FINALIZADO')"));

        builder.HasKey(x => x.Id).HasName("pk_agendamento_historico");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.AgendamentoId).IsRequired();
        builder.Property(x => x.EventoRaw)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("evento");
        builder.Property(x => x.Payload).HasColumnType("jsonb");
        builder.Property(x => x.UsuarioId).IsRequired();
        builder.Property(x => x.OcorridoEm)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.Ignore(x => x.Evento);

        builder.HasOne<Agendamento>()
            .WithMany()
            .HasForeignKey(x => x.AgendamentoId)
            .HasConstraintName("fk_hist_agendamento")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .HasConstraintName("fk_hist_usuario")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.AgendamentoId, x.OcorridoEm })
            .HasDatabaseName("idx_hist_agendamento")
            .IsDescending(false, true);
        builder.HasIndex(x => x.EventoRaw).HasDatabaseName("idx_hist_evento");
        builder.HasIndex(x => x.OcorridoEm).HasDatabaseName("idx_hist_ocorrido_em");
    }
}
