using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nome)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasMany(x => x.Veiculos)
            .WithOne(v => v.Cliente)
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
