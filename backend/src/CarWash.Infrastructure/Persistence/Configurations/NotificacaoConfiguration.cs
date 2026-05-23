using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public sealed class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
{
    public void Configure(EntityTypeBuilder<Notificacao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("notificacoes", t =>
        {
            t.HasCheckConstraint("ck_notif_canal", "canal IN ('email','whatsapp','sms')");
            t.HasCheckConstraint("ck_notif_tentativas", "tentativas >= 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_notificacoes");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.AgendamentoId).IsRequired();
        builder.Property(x => x.Tipo).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Canal).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Destino).IsRequired().HasMaxLength(120);
        builder.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pendente");
        builder.Property(x => x.Tentativas).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.UltimaTentativa).HasColumnType("timestamptz");
        builder.Property(x => x.CriadoEm)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.HasOne<Agendamento>()
            .WithMany()
            .HasForeignKey(x => x.AgendamentoId)
            .HasConstraintName("fk_notif_agendamento")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.AgendamentoId, x.Tipo, x.Canal, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("uk_notif_dedupe");

        builder.HasIndex(x => new { x.Status, x.CriadoEm })
            .HasDatabaseName("idx_notif_status");
    }
}
