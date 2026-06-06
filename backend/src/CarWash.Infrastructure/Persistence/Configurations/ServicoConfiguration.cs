using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public sealed class ServicoConfiguration : IEntityTypeConfiguration<Servico>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Servico> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("servicos", t =>
        {
            t.HasCheckConstraint("ck_servicos_preco", "preco > 0");
            t.HasCheckConstraint("ck_servicos_duracao_positiva", "duracao_min > 0");
            t.HasCheckConstraint("ck_servicos_duracao_max", $"duracao_min <= {Servico.DuracaoMinValorMax}");
        });

        builder.HasKey(x => x.Id).HasName("pk_servicos");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Nome).IsRequired().HasMaxLength(Servico.NomeMaxLength);
        builder.Property(x => x.Preco).IsRequired().HasColumnType("numeric(10,2)");
        builder.Property(x => x.DuracaoMin).IsRequired();
        builder.Property(x => x.Ativo).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CriadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(x => x.AtualizadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");

        builder.HasIndex(x => x.Nome)
            .IsUnique()
            .HasDatabaseName("uk_servicos_nome")
            .HasFilter(null);

        builder.HasIndex(x => x.Ativo)
            .HasDatabaseName("idx_servicos_ativo")
            .HasFilter("ativo = true");
    }
}
