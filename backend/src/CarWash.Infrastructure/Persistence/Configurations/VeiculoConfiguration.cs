using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public class VeiculoConfiguration : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> builder)
    {
        builder.ToTable("veiculos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ClienteId)
            .HasColumnName("cliente_id")
            .IsRequired();

        builder.Property(x => x.Placa)
            .HasColumnName("placa")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.Modelo)
            .HasColumnName("modelo")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.Fabricante)
            .HasColumnName("fabricante")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.Cor)
            .HasColumnName("cor")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Ano)
            .HasColumnName("ano");

        builder.Property(x => x.Ativo)
            .HasColumnName("ativo")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.AtualizadoEm)
            .HasColumnName("atualizado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.Cliente)
            .WithMany()
            .HasForeignKey(x => x.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Placa)
            .IsUnique()
            .HasDatabaseName("uk_veiculos_placa");

        builder.HasIndex(x => x.ClienteId)
            .HasDatabaseName("idx_veiculos_cliente_id");

        builder.HasCheckConstraint(
            "ck_veiculos_placa_formato",
            "placa ~ '^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$'");
    }
}
