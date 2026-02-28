using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using UserManagement.Domain.Factories;
using UserManagement.Domain.ValueObjects;
using UserManagement.Infrastructure.Persistence;
using UserManagement.Infrastructure.Persistence.Repositories;
using Xunit;

namespace UserManagement.IntegrationTests;

/// <summary>
/// Milestone 04-infrastructure-data: validates EF Core persistence with MySQL (Testcontainers),
/// repository operations, and soft delete query filter.
/// </summary>
public sealed class PersistenceTests : IAsyncLifetime
{
    private MySqlContainer? _mySqlContainer;
    private MySqlDbContext? _dbContext;

    public async Task InitializeAsync()
    {
        _mySqlContainer = new MySqlBuilder()
            .WithImage("mysql:8.0")
            .Build();

        await _mySqlContainer.StartAsync();

        var options = new DbContextOptionsBuilder<MySqlDbContext>()
            .UseMySql(_mySqlContainer.GetConnectionString(), ServerVersion.AutoDetect(_mySqlContainer.GetConnectionString()))
            .Options;

        _dbContext = new MySqlDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_dbContext != null)
        {
            await _dbContext.DisposeAsync();
        }

        if (_mySqlContainer != null)
        {
            await _mySqlContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task UserRepository_AddAndGetByEmail_ShouldPersistAndRetrieve()
    {
        var repository = new UserRepository(_dbContext!);
        var email = Email.Create("test@example.com");
        var user = UserFactory.CreatePending(email, "Test User");

        await repository.AddAsync(user);
        await _dbContext!.SaveChangesAsync();

        var retrieved = await repository.GetByEmailAsync(email);

        retrieved.Should().NotBeNull();
        retrieved!.Email.Value.Should().Be("test@example.com");
        retrieved.Name.Should().Be("Test User");
        retrieved.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task UserRepository_SoftDelete_ShouldFilterDeletedUsers()
    {
        var repository = new UserRepository(_dbContext!);
        var email = Email.Create("deleted@example.com");
        var user = UserFactory.CreatePending(email, "To Delete");

        await repository.AddAsync(user);
        await _dbContext!.SaveChangesAsync();

        user.Delete();
        await repository.UpdateAsync(user);
        await _dbContext!.SaveChangesAsync();

        var retrievedByEmail = await repository.GetByEmailAsync(email);
        var allUsers = await repository.GetAllAsync();

        retrievedByEmail.Should().BeNull("soft deleted users should not be returned by GetByEmailAsync");
        allUsers.Should().NotContain(u => u.Id == user.Id, "soft deleted users should not be in GetAllAsync");
    }

    [Fact]
    public async Task UserRepository_ExistsByEmail_ShouldRespectSoftDelete()
    {
        var repository = new UserRepository(_dbContext!);
        var email = Email.Create("exists@example.com");
        var user = UserFactory.CreatePending(email, "Exists Test");

        await repository.AddAsync(user);
        await _dbContext!.SaveChangesAsync();

        var existsBeforeDelete = await repository.ExistsByEmailAsync(email);
        existsBeforeDelete.Should().BeTrue();

        user.Delete();
        await repository.UpdateAsync(user);
        await _dbContext!.SaveChangesAsync();

        var existsAfterDelete = await repository.ExistsByEmailAsync(email);
        existsAfterDelete.Should().BeFalse("soft deleted users should not exist");
    }

    [Fact]
    public async Task UserOtpRepository_AddAndGetByEmailAndCode_ShouldWork()
    {
        var repository = new UserOtpRepository(_dbContext!);
        var email = Email.Create("otp@example.com");
        var otp = Domain.Entities.UserOtp.Create(Guid.NewGuid(), email, "123456", TimeSpan.FromMinutes(10));

        await repository.AddAsync(otp);
        await _dbContext!.SaveChangesAsync();

        var retrieved = await repository.GetByEmailAndCodeAsync(email, "123456");

        retrieved.Should().NotBeNull();
        retrieved!.Code.Should().Be("123456");
        retrieved.Email.Value.Should().Be("otp@example.com");
    }
}
