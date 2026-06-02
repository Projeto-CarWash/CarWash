using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public sealed class ResponsavelConfiguration : IEntityTypeConfiguration<Responsavel>
{
    public void Configure(EntityTypeBuilder<Responsavel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("responsaveis", t => t.HasCheckConstraint(
            "ck_responsaveis_grau_vinculo",
            "grau_vinculo IN ('RESPONSAVEL_FINANCEIRO','RESPONSAVEL_LEGAL','PROCURADOR','CONJUGE','PAI_MAE','OUTRO')"));

        builder.HasKey(x => x.Id).HasName("pk_responsaveis");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ClienteTitularId).IsRequired();
        builder.Property(x => x.Nome).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Documento).IsRequired().HasMaxLength(14);
        builder.Property(x => x.Telefone).HasMaxLength(11);
        builder.Property(x => x.Email).HasMaxLength(150);
        builder.Property(x => x.GrauVinculo).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Ativo).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CriadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(x => x.AtualizadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");

        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(x => x.ClienteTitularId)
            .HasConstraintName("fk_responsaveis_cliente_titular")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Documento)
            .IsUnique()
            .HasDatabaseName("uk_responsaveis_documento");

        builder.HasIndex(x => x.ClienteTitularId)
            .HasDatabaseName("idx_responsaveis_cliente_titular_id");
    }
}
