using FluentAssertions;
using UserManagement.Application.Common.Results;
using UserManagement.Application.Features.Users.Commands.VerifyOtp;
using UserManagement.Application.UnitTests.Fakes;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Factories;
using UserManagement.Domain.ValueObjects;
using Xunit;

namespace UserManagement.Application.UnitTests.VerifyOtp;

public class VerifyOtpHandlerTests
{
    [Fact]
    public async Task Handle_When_Otp_Not_Found_Returns_OtpInvalid()
    {
        var userRepo = new FakeUserRepository();
        var otpRepo = new FakeUserOtpRepository();
        var dateTime = new FakeDateTimeProvider();
        var cognito = new FakeCognitoIdentityService();

        var user = UserFactory.CreatePending(Email.Create("user@example.com"), "User");
        await userRepo.AddAsync(user, CancellationToken.None);

        var handler = new VerifyOtpCommandHandler(userRepo, otpRepo, dateTime, cognito, new FakeUnitOfWork());
        var command = new VerifyOtpCommand("user@example.com", "000000", Guid.NewGuid());
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(Error.Codes.OtpInvalid);
    }

    [Fact]
    public async Task Handle_When_Otp_Expired_Returns_OtpExpired()
    {
        var email = Email.Create("user@example.com");
        var user = UserFactory.CreatePending(email, "User");
        var otp = UserOtp.Create(Guid.NewGuid(), email, "123456", TimeSpan.FromMinutes(-1));

        var userRepo = new FakeUserRepository();
        await userRepo.AddAsync(user, CancellationToken.None);

        var otpRepo = new FakeUserOtpRepository();
        await otpRepo.AddAsync(otp, CancellationToken.None);

        var dateTime = new FakeDateTimeProvider { UtcNow = DateTimeOffset.UtcNow };
        var cognito = new FakeCognitoIdentityService();

        var handler = new VerifyOtpCommandHandler(userRepo, otpRepo, dateTime, cognito, new FakeUnitOfWork());
        var command = new VerifyOtpCommand("user@example.com", "123456", Guid.NewGuid());
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(Error.Codes.OtpExpired);
    }
}
