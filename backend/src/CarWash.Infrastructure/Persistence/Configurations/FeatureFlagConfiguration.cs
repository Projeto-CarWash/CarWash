using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public sealed class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("feature_flags");

        builder.HasKey(x => x.Id).HasName("pk_feature_flags");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Nome).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Ambiente).IsRequired().HasMaxLength(30);
        builder.Property(x => x.FilialId);
        builder.Property(x => x.Habilitada).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.ValorJson).HasColumnType("jsonb");
        builder.Property(x => x.AtualizadoPor).IsRequired();
        builder.Property(x => x.AtualizadoEm)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.HasOne<Filial>()
            .WithMany()
            .HasForeignKey(x => x.FilialId)
            .HasConstraintName("fk_flag_filial")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.AtualizadoPor)
            .HasConstraintName("fk_flag_atualizado_por")
            .OnDelete(DeleteBehavior.Restrict);

        // unique lógico em (nome, ambiente, COALESCE(filial_id, ...)) — criado via SQL na migration.
    }
}
