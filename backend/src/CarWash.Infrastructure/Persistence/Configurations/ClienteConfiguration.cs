using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Nome)
            .HasColumnName("nome")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Cpf)
            .HasColumnName("cpf")
            .HasMaxLength(11);

        builder.Property(x => x.Cnpj)
            .HasColumnName("cnpj")
            .HasMaxLength(14);

        builder.Property(x => x.Telefone)
            .HasColumnName("telefone")
            .HasMaxLength(11);

        builder.Property(x => x.Celular)
            .HasColumnName("celular")
            .HasMaxLength(11);

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(150);

        builder.Property(x => x.Endereco)
            .HasColumnName("endereco")
            .HasMaxLength(255);

        builder.Property(x => x.Observacoes)
            .HasColumnName("observacoes")
            .HasColumnType("text");

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

        builder.HasCheckConstraint(
            "ck_clientes_cpf_ou_cnpj",
            "(cpf is not null and cnpj is null) or (cpf is null and cnpj is not null)");

        builder.HasCheckConstraint(
            "ck_clientes_telefone_somente_digitos",
            "telefone IS NULL OR telefone ~ '^[0-9]{10,11}$'");

        builder.HasCheckConstraint(
            "ck_clientes_celular_somente_digitos",
            "celular IS NULL OR celular ~ '^[0-9]{11}$'");

        builder.HasIndex(x => x.Nome)
            .HasDatabaseName("idx_clientes_nome");

        builder.HasIndex(x => x.Email)
            .HasDatabaseName("idx_clientes_email");

        builder.HasIndex(x => x.Cpf)
            .IsUnique()
            .HasFilter("cpf IS NOT NULL")
            .HasDatabaseName("uk_clientes_cpf");

        builder.HasIndex(x => x.Cnpj)
            .IsUnique()
            .HasFilter("cnpj IS NOT NULL")
            .HasDatabaseName("uk_clientes_cnpj");
    }
}
