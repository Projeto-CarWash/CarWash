using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public sealed class OutboxEventoConfiguration : IEntityTypeConfiguration<OutboxEvento>
{
    public void Configure(EntityTypeBuilder<OutboxEvento> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("outbox_eventos", t =>
        {
            t.HasCheckConstraint(
                "ck_outbox_status",
                "status IN ('pendente','processando','processado','falha')");
            t.HasCheckConstraint("ck_outbox_tentativas", "tentativas >= 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_outbox_eventos");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Evento).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Agregado).IsRequired().HasMaxLength(80);
        builder.Property(x => x.AgregadoId).IsRequired();
        builder.Property(x => x.Payload).IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(120);
        builder.Property(x => x.StatusRaw)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("status")
            .HasDefaultValue("pendente");
        builder.Property(x => x.Tentativas).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.DisponivelEm)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
        builder.Property(x => x.CriadoEm)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
        builder.Property(x => x.ProcessadoEm).HasColumnType("timestamptz");

        builder.Ignore(x => x.Status);

        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("uk_outbox_idempotency");

        builder.HasIndex(x => new { x.StatusRaw, x.DisponivelEm })
            .HasDatabaseName("idx_outbox_status_disponivel")
            .HasFilter("status IN ('pendente','falha')");
    }
}
