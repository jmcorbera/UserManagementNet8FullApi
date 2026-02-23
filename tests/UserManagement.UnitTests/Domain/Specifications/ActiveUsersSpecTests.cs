using FluentAssertions;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Specifications;
using UserManagement.Domain.ValueObjects;
using Xunit;

namespace UserManagement.UnitTests.Domain.Specifications;

public class ActiveUsersSpecTests
{
    [Fact]
    public void IsSatisfiedBy_With_Active_User_And_Not_Deleted_Should_Return_True()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var spec = new ActiveUsersSpec();
        var user = User.CreatePending(Guid.NewGuid(), email, "Test User");
        user.Activate();

        // Act
        var result = spec.IsSatisfiedBy(user);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_With_PendingVerification_User_Should_Return_False()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var spec = new ActiveUsersSpec();
        var user = User.CreatePending(Guid.NewGuid(), email, "Test User");

        // Act
        var result = spec.IsSatisfiedBy(user);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_With_Deleted_User_Should_Return_False()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var spec = new ActiveUsersSpec();
        var user = User.CreatePending(Guid.NewGuid(), email, "Test User");
        user.Activate();
        user.Delete();

        // Act
        var result = spec.IsSatisfiedBy(user);

        // Assert
        result.Should().BeFalse();
    }
}
