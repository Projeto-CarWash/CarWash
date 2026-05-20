using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public class VeiculoConfiguration : IEntityTypeConfiguration<Veiculo>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Veiculo> builder)
    {
        builder.ToTable("veiculos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Placa)
            .IsRequired()
            .HasMaxLength(7);

        builder.HasIndex(x => x.Placa)
            .IsUnique();

        builder.Property(x => x.Modelo)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.Fabricante)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.Cor)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.CreatedAt)
            .IsRequired();
    }
}
