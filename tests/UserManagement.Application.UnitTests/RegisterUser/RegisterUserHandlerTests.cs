using FluentAssertions;
using Microsoft.Extensions.Options;
using UserManagement.Application.Common.Options;
using UserManagement.Application.Common.Results;
using UserManagement.Application.Features.Users.Commands.RegisterUser;
using UserManagement.Application.UnitTests.Fakes;
using UserManagement.Domain.ValueObjects;
using Xunit;

namespace UserManagement.Application.UnitTests.RegisterUser;

public class RegisterUserHandlerTests
{
    [Fact]
    public async Task Handle_When_Email_Exists_Returns_Conflict()
    {
        var userRepo = new FakeUserRepository();
        var email = Email.Create("existing@example.com");
        userRepo.SeedExistingEmail(email);

        var handler = new RegisterUserCommandHandler(
            userRepo,
            new FakeUserOtpRepository(),
            new FakeEmailSender(),
            new FakeOtpGenerator(),
            Options.Create(new FeatureFlagsOptions { EnableOtp = true }));

        var command = new RegisterUserCommand("existing@example.com", "Test User");
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(Error.Codes.Conflict);
        result.Error.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_When_EnableOtp_True_Generates_Otp_And_Calls_EmailSender()
    {
        var userRepo = new FakeUserRepository();
        var otpRepo = new FakeUserOtpRepository();
        var emailSender = new FakeEmailSender();
        var otpGenerator = new FakeOtpGenerator { NextCode = "999888" };

        var handler = new RegisterUserCommandHandler(
            userRepo,
            otpRepo,
            emailSender,
            otpGenerator,
            Options.Create(new FeatureFlagsOptions { EnableOtp = true }));

        var command = new RegisterUserCommand("newuser@example.com", "New User");
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        emailSender.Sent.Should().HaveCount(1);
        emailSender.Sent[0].To.Should().Be("newuser@example.com");
        emailSender.Sent[0].Body.Should().Contain("999888");
    }

    [Fact]
    public async Task Handle_When_EnableOtp_False_Returns_FeatureDisabled()
    {
        var handler = new RegisterUserCommandHandler(
            new FakeUserRepository(),
            new FakeUserOtpRepository(),
            new FakeEmailSender(),
            new FakeOtpGenerator(),
            Options.Create(new FeatureFlagsOptions { EnableOtp = false }));

        var command = new RegisterUserCommand("user@example.com", "User");
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(Error.Codes.FeatureDisabled);
    }
}
