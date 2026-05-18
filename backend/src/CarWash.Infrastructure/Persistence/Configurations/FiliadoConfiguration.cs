using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public sealed class FiliadoConfiguration : IEntityTypeConfiguration<Filiado>
{
    public void Configure(EntityTypeBuilder<Filiado> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("filiados", t =>
            t.HasCheckConstraint(
                "ck_filiados_cpf_ou_rg",
                "cpf IS NOT NULL OR rg IS NOT NULL"));

        builder.HasKey(x => x.Id).HasName("pk_filiados");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ClienteId).IsRequired();
        builder.Property(x => x.Nome).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Telefone).IsRequired().HasMaxLength(11);
        builder.Property(x => x.Cpf).HasMaxLength(11);
        builder.Property(x => x.Rg).HasMaxLength(20);
        builder.Property(x => x.Ativo).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CriadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(x => x.AtualizadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");

        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(x => x.ClienteId)
            .HasConstraintName("fk_filiados_cliente")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ClienteId).HasDatabaseName("idx_filiados_cliente_id");

        builder.HasIndex(x => x.Cpf)
            .HasDatabaseName("idx_filiados_cpf")
            .HasFilter("cpf IS NOT NULL");

        builder.HasIndex(x => new { x.ClienteId, x.Cpf })
            .IsUnique()
            .HasDatabaseName("uk_filiados_cliente_cpf")
            .HasFilter("cpf IS NOT NULL");
    }
}
