using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration de <see cref="Filial"/> (RF017 + RF018). Espelha o padrão de
/// <see cref="ClienteConfiguration"/>: endereço em colunas planas com
/// <c>builder.Ignore(x =&gt; x.Endereco)</c>. Constraints/índices finais
/// detalhados no ADR-0007 §3.2.
/// </summary>
public sealed class FilialConfiguration : IEntityTypeConfiguration<Filial>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Filial> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("filiais", t =>
        {
            t.HasCheckConstraint(
                "ck_filiais_celulas_faixa",
                "celulas_ativas BETWEEN 1 AND 100");

            // RF017 — ADR-0007 §3.2: aceita NULL durante rollout aditivo
            // (card 207 fechará para NOT NULL após backfill).
            t.HasCheckConstraint(
                "ck_filiais_codigo_formato",
                "codigo IS NULL OR codigo ~ '^[A-Z0-9]{2,20}$'");
        });

        builder.HasKey(x => x.Id).HasName("pk_filiais");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Nome).IsRequired().HasMaxLength(120);

        // RF017 — rollout aditivo (ADR-0007 §3.1). O domínio exige codigo
        // (string não-nullable), mas a coluna fica nullable em produção até o
        // card 207 endurecer para NOT NULL após backfill manual. Forçamos
        // IsRequired(false) para refletir o DDL real e evitar divergência de
        // schema entre o EF e a migration aditiva.
        builder.Property(x => x.Codigo).HasMaxLength(20).IsRequired(false);

        builder.Property(x => x.Cnpj).HasMaxLength(14);
        builder.Property(x => x.Ativa).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CelulasAtivas).IsRequired();
        builder.Property(x => x.Timezone)
            .IsRequired()
            .HasMaxLength(64)
            .HasDefaultValue("America/Sao_Paulo");

        // Endereço estruturado (RF017 §3.1): colunas planas + Ignore do getter
        // computado. Mesma estratégia do agregado Cliente — ADR-0007 §3.1.
        builder.Property(x => x.EnderecoCep).HasMaxLength(8);
        builder.Property(x => x.EnderecoLogradouro).HasMaxLength(150);
        builder.Property(x => x.EnderecoNumero).HasMaxLength(20);
        builder.Property(x => x.EnderecoComplemento).HasMaxLength(100);
        builder.Property(x => x.EnderecoBairro).HasMaxLength(100);
        builder.Property(x => x.EnderecoCidade).HasMaxLength(100);
        builder.Property(x => x.EnderecoUf).HasMaxLength(2).IsFixedLength();

        builder.Ignore(x => x.Endereco);

        builder.Property(x => x.CriadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(x => x.AtualizadoEm).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");

        // Auditoria (RF017 ADR-0007 §2.5) — análoga a Cliente.criado_por_usuario_id.
        builder.Property(x => x.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id");

        // ADR-0007 §3.2 — UNIQUE funcional case-insensitive em LOWER(nome).
        // O índice funcional é criado por raw SQL na migration AdicionaCadastroFilial
        // (EF Core não tem suporte nativo a índice em expressão). Aqui apenas
        // garantimos que o snapshot conheça a coluna; o índice em si vive na migration
        // e fica fora do modelo do EF (não declaramos HasIndex em Nome para evitar
        // divergência com o DDL real).

        // UNIQUE parcial em codigo — aceita NULL durante rollout aditivo.
        builder.HasIndex(x => x.Codigo)
            .IsUnique()
            .HasDatabaseName("uk_filiais_codigo")
            .HasFilter("codigo IS NOT NULL");

        // UNIQUE parcial em cnpj — opcional (L2 do ADR-0007).
        builder.HasIndex(x => x.Cnpj)
            .IsUnique()
            .HasDatabaseName("uk_filiais_cnpj")
            .HasFilter("cnpj IS NOT NULL");

        builder.HasIndex(x => x.Ativa)
            .HasDatabaseName("idx_filiais_ativa")
            .HasFilter("ativa = true");

        // Índice composto para busca por cidade/UF (GET listagem).
        builder.HasIndex(x => new { x.EnderecoCidade, x.EnderecoUf })
            .HasDatabaseName("idx_filiais_cidade_uf")
            .HasFilter("endereco_cidade IS NOT NULL");
    }
}
