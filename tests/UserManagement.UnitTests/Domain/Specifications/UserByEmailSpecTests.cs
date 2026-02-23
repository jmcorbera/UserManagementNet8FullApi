using FluentAssertions;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Specifications;
using UserManagement.Domain.ValueObjects;
using Xunit;

namespace UserManagement.UnitTests.Domain.Specifications;

public class UserByEmailSpecTests
{
    [Fact]
    public void IsSatisfiedBy_With_Matching_Email_And_Not_Deleted_Should_Return_True()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var spec = new UserByEmailSpec(email);
        var user = User.CreatePending(Guid.NewGuid(), email, "Test User");

        // Act
        var result = spec.IsSatisfiedBy(user);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_With_Different_Email_Should_Return_False()
    {
        // Arrange
        var email1 = Email.Create("test1@example.com");
        var email2 = Email.Create("test2@example.com");
        var spec = new UserByEmailSpec(email1);
        var user = User.CreatePending(Guid.NewGuid(), email2, "Test User");

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
        var spec = new UserByEmailSpec(email);
        var user = User.CreatePending(Guid.NewGuid(), email, "Test User");
        user.Delete();

        // Act
        var result = spec.IsSatisfiedBy(user);

        // Assert
        result.Should().BeFalse();
    }
}
