using FluentAssertions;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Enums;
using UserManagement.Domain.Specifications;
using UserManagement.Domain.ValueObjects;
using Xunit;

namespace UserManagement.UnitTests.Domain.Specifications;

public class UsersPaginatedSpecTests
{
    [Fact]
    public void IsSatisfiedBy_With_No_Filters_Should_Return_True_For_Any_User()
    {
        // Arrange
        var spec = new UsersPaginatedSpec();
        var email = Email.Create("test@example.com");
        var user = User.CreatePending(Guid.NewGuid(), email, "Test User");

        // Act
        var result = spec.IsSatisfiedBy(user);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_With_StatusFilter_Active_Should_Return_True_For_Active_User()
    {
        // Arrange
        var spec = new UsersPaginatedSpec(statusFilter: UserStatus.Active);
        var email = Email.Create("test@example.com");
        var user = User.CreatePending(Guid.NewGuid(), email, "Test User");
        user.Activate();

        // Act
        var result = spec.IsSatisfiedBy(user);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_With_StatusFilter_Active_Should_Return_False_For_Pending_User()
    {
        // Arrange
        var spec = new UsersPaginatedSpec(statusFilter: UserStatus.Active);
        var email = Email.Create("test@example.com");
        var user = User.CreatePending(Guid.NewGuid(), email, "Test User");

        // Act
        var result = spec.IsSatisfiedBy(user);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_With_IncludeDeleted_False_Should_Return_False_For_Deleted_User()
    {
        // Arrange
        var spec = new UsersPaginatedSpec(includeDeleted: false);
        var email = Email.Create("test@example.com");
        var user = User.CreatePending(Guid.NewGuid(), email, "Test User");
        user.Delete();

        // Act
        var result = spec.IsSatisfiedBy(user);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_With_IncludeDeleted_True_Should_Return_True_For_Deleted_User()
    {
        // Arrange
        var spec = new UsersPaginatedSpec(includeDeleted: true);
        var email = Email.Create("test@example.com");
        var user = User.CreatePending(Guid.NewGuid(), email, "Test User");
        user.Delete();

        // Act
        var result = spec.IsSatisfiedBy(user);

        // Assert
        result.Should().BeTrue();
    }
}
