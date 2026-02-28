using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagement.Domain.Entities;

namespace UserManagement.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("IdempotencyKeys");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Key)
            .IsRequired();
            
        builder.Property(i => i.CommandName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.Property(i => i.CompletedAt);

        builder.Property(i => i.Result)
            .HasColumnType("longtext");

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(i => i.Key)
            .IsUnique()
            .HasDatabaseName("IX_IdempotencyKeys_Key");

        builder.HasIndex(i => i.CreatedAt)
            .HasDatabaseName("IX_IdempotencyKeys_CreatedAt");
    }
}
