using Asisya.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asisya.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.Username).IsUnique();
        builder.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
    }
}
