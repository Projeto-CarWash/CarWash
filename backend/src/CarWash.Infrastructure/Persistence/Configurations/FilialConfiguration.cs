using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public sealed class FilialConfiguration : IEntityTypeConfiguration<Filial>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Filial> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("filiais", t =>
            t.HasCheckConstraint(
                "ck_filiais_celulas_faixa",
                "celulas_ativas BETWEEN 1 AND 100"));

        builder.HasKey(x => x.Id).HasName("pk_filiais");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Nome).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Ativa).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CelulasAtivas).IsRequired();
        builder.Property(x => x.Timezone)
            .IsRequired()
            .HasMaxLength(64)
            .HasDefaultValue("America/Sao_Paulo");
        builder.Property(x => x.CriadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(x => x.AtualizadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");

        builder.HasIndex(x => x.Nome)
            .IsUnique()
            .HasDatabaseName("uk_filiais_nome");

        builder.HasIndex(x => x.Ativa)
            .HasDatabaseName("idx_filiais_ativa")
            .HasFilter("ativa = true");
    }
}
