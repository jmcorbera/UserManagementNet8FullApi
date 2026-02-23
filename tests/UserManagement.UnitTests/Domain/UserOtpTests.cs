using FluentAssertions;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Common;
using UserManagement.Domain.ValueObjects;
using Xunit;

namespace UserManagement.UnitTests.Domain;

public class UserOtpTests
{
    [Fact]
    public void IsValid_When_Not_Used_And_Not_Expired_Should_Return_True()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var otp = UserOtp.Create(
            Guid.NewGuid(),
            email,
            "123456",
            TimeSpan.FromMinutes(10)
        );

        // Act
        var isValid = otp.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_When_Used_Should_Return_False()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var otp = UserOtp.Create(
            Guid.NewGuid(),
            email,
            "123456",
            TimeSpan.FromMinutes(10)
        );
        otp.MarkAsUsed();

        // Act
        var isValid = otp.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_When_Expired_Should_Return_False()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var otp = UserOtp.Create(
            Guid.NewGuid(),
            email,
            "123456",
            TimeSpan.FromMinutes(-1) // Expired
        );

        // Act
        var isValid = otp.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void MarkAsUsed_Should_Set_Used_To_True()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var otp = UserOtp.Create(
            Guid.NewGuid(),
            email,
            "123456",
            TimeSpan.FromMinutes(10)
        );

        // Act
        otp.MarkAsUsed();

        // Assert
        otp.Used.Should().BeTrue();
    }

    [Fact]
    public void MarkAsUsed_When_Already_Used_Should_Throw()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var otp = UserOtp.Create(
            Guid.NewGuid(),
            email,
            "123456",
            TimeSpan.FromMinutes(10)
        );
        otp.MarkAsUsed();

        // Act & Assert
        var act = () => otp.MarkAsUsed();
        act.Should().Throw<DomainException>();
    }
}
