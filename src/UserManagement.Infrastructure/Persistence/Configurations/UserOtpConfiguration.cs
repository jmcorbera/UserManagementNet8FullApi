using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UserManagement.Domain.Entities;
using UserManagement.Domain.ValueObjects;

namespace UserManagement.Infrastructure.Persistence.Configurations;

public sealed class UserOtpConfiguration : IEntityTypeConfiguration<UserOtp>
{
    public void Configure(EntityTypeBuilder<UserOtp> builder)
    {
        builder.ToTable("UserOtps");

        builder.HasKey(o => o.Id);

        var emailConverter = new ValueConverter<Email, string>(
            v => v.Value,
            v => Email.Create(v));

        builder.Property(o => o.Email)
            .HasConversion(emailConverter)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(o => o.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.ExpiresAt)
            .IsRequired();

        builder.Property(o => o.Used)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.HasIndex(o => new { o.Email, o.Code });
        builder.HasIndex(o => new { o.Email, o.CreatedAt });
    }
}
