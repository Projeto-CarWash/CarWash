using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public sealed class VeiculoConfiguration : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("veiculos", t =>
            t.HasCheckConstraint(
                "ck_veiculos_ano",
                "ano IS NULL OR (ano BETWEEN 1900 AND 2100)"));

        builder.HasKey(x => x.Id).HasName("pk_veiculos");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ClienteId).IsRequired();
        builder.Property(x => x.Placa).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Modelo).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Fabricante).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Cor).IsRequired().HasMaxLength(40);
        builder.Property(x => x.Ano);
        builder.Property(x => x.Ativo).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CriadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(x => x.AtualizadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");

        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(x => x.ClienteId)
            .HasConstraintName("fk_veiculos_cliente")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Placa)
            .IsUnique()
            .HasDatabaseName("uk_veiculos_placa");

        builder.HasIndex(x => x.ClienteId).HasDatabaseName("idx_veiculos_cliente_id");
    }
}
