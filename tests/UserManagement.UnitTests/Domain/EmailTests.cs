using FluentAssertions;
using UserManagement.Domain.Common;
using UserManagement.Domain.ValueObjects;
using Xunit;

namespace UserManagement.UnitTests.Domain;

public class EmailTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@example.co.uk")]
    [InlineData("user+tag@example.com")]
    public void Create_With_Valid_Email_Should_Succeed(string emailValue)
    {
        // Act
        var email = Email.Create(emailValue);

        // Assert
        email.Should().NotBeNull();
        email.Value.Should().Be(emailValue.ToLowerInvariant().Trim());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Null_Or_Empty_Should_Throw(string? emailValue)
    {
        // Act & Assert
        var act = () => Email.Create(emailValue!);
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user @example.com")]
    public void Create_With_Invalid_Format_Should_Throw(string emailValue)
    {
        // Act & Assert
        var act = () => Email.Create(emailValue);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Should_Normalize_Email_To_Lowercase()
    {
        // Arrange
        var emailValue = "Test@Example.COM";

        // Act
        var email = Email.Create(emailValue);

        // Assert
        email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Equals_Should_Return_True_For_Same_Email()
    {
        // Arrange
        var email1 = Email.Create("test@example.com");
        var email2 = Email.Create("test@example.com");

        // Act & Assert
        email1.Equals(email2).Should().BeTrue();
    }

    [Fact]
    public void Equals_Should_Return_False_For_Different_Emails()
    {
        // Arrange
        var email1 = Email.Create("test1@example.com");
        var email2 = Email.Create("test2@example.com");

        // Act & Assert
        email1.Equals(email2).Should().BeFalse();
    }

    [Fact]
    public void ToString_Should_Return_Email_Value()
    {
        // Arrange
        var emailValue = "test@example.com";
        var email = Email.Create(emailValue);

        // Act
        var result = email.ToString();

        // Assert
        result.Should().Be(emailValue);
    }

    [Fact]
    public void Implicit_Conversion_To_String_Should_Work()
    {
        // Arrange
        var email = Email.Create("test@example.com");

        // Act
        string emailString = email;

        // Assert
        emailString.Should().Be("test@example.com");
    }
}
