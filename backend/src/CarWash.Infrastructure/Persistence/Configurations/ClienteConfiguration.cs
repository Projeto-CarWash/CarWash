using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("clientes", t =>
            t.HasCheckConstraint(
                "ck_clientes_cpf_ou_cnpj",
                "cpf IS NOT NULL OR cnpj IS NOT NULL"));

        builder.HasKey(x => x.Id).HasName("pk_clientes");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Nome).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DataNascimento).IsRequired().HasColumnType("date");
        builder.Property(x => x.Cpf).HasMaxLength(11);
        builder.Property(x => x.Cnpj).HasMaxLength(14);
        builder.Property(x => x.Telefone).HasMaxLength(11);
        builder.Property(x => x.Celular).IsRequired().HasMaxLength(11);
        builder.Property(x => x.Email).HasMaxLength(150);

        builder.Property(x => x.EnderecoCep).IsRequired().HasMaxLength(8);
        builder.Property(x => x.EnderecoLogradouro).IsRequired().HasMaxLength(150);
        builder.Property(x => x.EnderecoNumero).IsRequired().HasMaxLength(20);
        builder.Property(x => x.EnderecoComplemento).HasMaxLength(100);
        builder.Property(x => x.EnderecoBairro).IsRequired().HasMaxLength(100);
        builder.Property(x => x.EnderecoCidade).IsRequired().HasMaxLength(100);
        builder.Property(x => x.EnderecoUf).IsRequired().HasMaxLength(2).IsFixedLength();

        builder.Ignore(x => x.Endereco);

        builder.Property(x => x.Ativo).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CriadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(x => x.AtualizadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");

        builder.HasIndex(x => x.Nome).HasDatabaseName("idx_clientes_nome");

        builder.HasIndex(x => x.Email)
            .HasDatabaseName("idx_clientes_email")
            .HasFilter("email IS NOT NULL");

        builder.HasIndex(x => x.Cpf)
            .IsUnique()
            .HasDatabaseName("uk_clientes_cpf")
            .HasFilter("cpf IS NOT NULL");

        builder.HasIndex(x => x.Cnpj)
            .IsUnique()
            .HasDatabaseName("uk_clientes_cnpj")
            .HasFilter("cnpj IS NOT NULL");
    }
}
