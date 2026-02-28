using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagement.Domain.Entities;

namespace UserManagement.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Type)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.Content)
            .IsRequired();

        builder.Property(o => o.OccurredAt)
            .IsRequired();

        builder.Property(o => o.ProcessedAt);

        builder.Property(o => o.Error)
            .HasMaxLength(2000);

        builder.HasIndex(o => o.ProcessedAt)
            .HasDatabaseName("IX_OutboxMessages_ProcessedAt");

        builder.HasIndex(o => o.OccurredAt)
            .HasDatabaseName("IX_OutboxMessages_OccurredAt");
    }
}
