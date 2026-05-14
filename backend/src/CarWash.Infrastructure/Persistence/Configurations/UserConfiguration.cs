using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarWash.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.Property(x => x.PasswordHash)
            .IsRequired();

        builder.Property(x => x.Active)
            .HasDefaultValue(true);

        builder.Property(x => x.FailedLoginAttempts)
            .HasDefaultValue(0);
    }
}
