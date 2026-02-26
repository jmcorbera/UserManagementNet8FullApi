using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UserManagement.Domain.Entities;
using UserManagement.Domain.ValueObjects;

namespace UserManagement.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        var emailConverter = new ValueConverter<Email, string>(
            v => v.Value,
            v => Email.Create(v));

        builder.Property(u => u.Email)
            .HasConversion(emailConverter)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(u => u.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.Status)
            .IsRequired();

        builder.Property(u => u.CognitoSub)
            .HasMaxLength(100);

        builder.Property(u => u.IsDeleted)
            .IsRequired();

        builder.Property(u => u.Created)
            .IsRequired();

        builder.Property(u => u.LastModified)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasIndex(u => u.CognitoSub);

        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
