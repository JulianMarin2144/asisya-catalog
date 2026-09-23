using Asisya.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asisya.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.PhotoUrl).HasMaxLength(512).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
    }
}
