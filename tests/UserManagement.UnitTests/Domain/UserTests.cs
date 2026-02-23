using FluentAssertions;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Common;
using UserManagement.Domain.Enums;
using UserManagement.Domain.ValueObjects;
using Xunit;

namespace UserManagement.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void Activate_Should_Change_Status_To_Active()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var user = User.CreatePending(Guid.NewGuid(), email, "Test User");

        // Act
        user.Activate();

        // Assert
        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void Activate_When_Already_Active_Should_Not_Throw()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var user = User.CreatePending(Guid.NewGuid(), email, "Test User");
        user.Activate();

        // Act
        var act = () => user.Activate();

        // Assert
        act.Should().NotThrow();
        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void SetCognitoSub_Should_Set_CognitoSub()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var user = User.CreatePending(Guid.NewGuid(), email, "Test User");
        var cognitoSub = "cognito-sub-123";

        // Act
        user.SetCognitoSub(cognitoSub);

        // Assert
        user.CognitoSub.Should().Be(cognitoSub);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetCognitoSub_With_Invalid_Value_Should_Throw(string? cognitoSub)
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var user = User.CreatePending(Guid.NewGuid(), email, "Test User");

        // Act & Assert
        var act = () => user.SetCognitoSub(cognitoSub!);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Delete_Should_Set_IsDeleted_To_True()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var user = User.CreatePending(Guid.NewGuid(), email, "Test User");

        // Act
        user.Delete();

        // Assert
        user.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Delete_When_Already_Deleted_Should_Not_Throw()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var user = User.CreatePending(Guid.NewGuid(), email, "Test User");
        user.Delete();

        // Act
        var act = () => user.Delete();

        // Assert
        act.Should().NotThrow();
        user.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void UpdateName_Should_Update_Name()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var user = User.CreatePending(Guid.NewGuid(), email, "Old Name");
        var newName = "New Name";

        // Act
        user.UpdateName(newName);

        // Assert
        user.Name.Should().Be(newName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateName_With_Invalid_Value_Should_Throw(string? name)
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var user = User.CreatePending(Guid.NewGuid(), email, "Test User");

        // Act & Assert
        var act = () => user.UpdateName(name!);
        act.Should().Throw<DomainException>();
    }
}
