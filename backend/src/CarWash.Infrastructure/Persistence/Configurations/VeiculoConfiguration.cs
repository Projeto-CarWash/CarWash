using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

<<<<<<< HEAD
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
=======
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
>>>>>>> d18fb68 (feat(clientes): adicionar veiculos no cadastro de cliente)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Placa)
            .IsUnique()
            .HasDatabaseName("uk_veiculos_placa");

<<<<<<< HEAD
        builder.HasIndex(x => x.ClienteId).HasDatabaseName("idx_veiculos_cliente_id");
=======
        builder.HasIndex(x => x.ClienteId)
            .HasDatabaseName("idx_veiculos_cliente_id");

        builder.HasCheckConstraint(
            "ck_veiculos_placa_formato",
            "placa ~ '^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$'");
>>>>>>> d18fb68 (feat(clientes): adicionar veiculos no cadastro de cliente)
    }
}
