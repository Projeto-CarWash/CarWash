using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public sealed class AgendamentoItemConfiguration : IEntityTypeConfiguration<AgendamentoItem>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<AgendamentoItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("agendamento_itens", t =>
        {
            t.HasCheckConstraint("ck_item_preco", "preco_aplicado >= 0");
            t.HasCheckConstraint("ck_item_duracao", "duracao_aplicada > 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_agendamento_itens");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.AgendamentoId).IsRequired();
        builder.Property(x => x.ServicoId).IsRequired();
        builder.Property(x => x.PrecoAplicado).IsRequired().HasColumnType("numeric(10,2)");
        builder.Property(x => x.DuracaoAplicada).IsRequired();
        builder.Property(x => x.CriadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");

        builder.HasOne<Agendamento>()
            .WithMany()
            .HasForeignKey(x => x.AgendamentoId)
            .HasConstraintName("fk_item_agendamento")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Servico>()
            .WithMany()
            .HasForeignKey(x => x.ServicoId)
            .HasConstraintName("fk_item_servico")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AgendamentoId).HasDatabaseName("idx_item_agendamento");
        builder.HasIndex(x => x.ServicoId).HasDatabaseName("idx_item_servico");
        builder.HasIndex(x => new { x.AgendamentoId, x.ServicoId })
            .IsUnique()
            .HasDatabaseName("uk_item_agendamento_servico");
    }
}
