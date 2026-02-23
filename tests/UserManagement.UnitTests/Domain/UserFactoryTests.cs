using FluentAssertions;
using UserManagement.Domain.Enums;
using UserManagement.Domain.Factories;
using UserManagement.Domain.ValueObjects;
using Xunit;

namespace UserManagement.UnitTests.Domain;

public class UserFactoryTests
{
    [Fact]
    public void CreatePending_Should_Create_User_With_PendingVerification_Status()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var name = "Test User";

        // Act
        var user = UserFactory.CreatePending(email, name);

        // Assert
        user.Should().NotBeNull();
        user.Status.Should().Be(UserStatus.PendingVerification);
        user.Email.Should().Be(email);
        user.Name.Should().Be(name);
        user.CognitoSub.Should().BeNull();
        user.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void FromCognito_Should_Create_User_With_Active_Status_And_CognitoSub()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var name = "Test User";
        var cognitoSub = "cognito-sub-123";

        // Act
        var user = UserFactory.FromCognito(email, name, cognitoSub);

        // Assert
        user.Should().NotBeNull();
        user.Status.Should().Be(UserStatus.Active);
        user.Email.Should().Be(email);
        user.Name.Should().Be(name);
        user.CognitoSub.Should().Be(cognitoSub);
        user.IsDeleted.Should().BeFalse();
    }
}
