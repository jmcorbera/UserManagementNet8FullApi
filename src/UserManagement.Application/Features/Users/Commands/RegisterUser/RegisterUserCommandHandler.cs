using MediatR;
using Microsoft.Extensions.Options;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Application.Common.Options;
using UserManagement.Application.Common.Results;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Factories;
using UserManagement.Domain.Repositories;
using UserManagement.Domain.ValueObjects;

namespace UserManagement.Application.Features.Users.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserOtpRepository _otpRepository;
    private readonly IEmailSender _emailSender;
    private readonly IOtpGenerator _otpGenerator;
    private readonly FeatureFlagsOptions _featureFlags;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IUserOtpRepository otpRepository,
        IEmailSender emailSender,
        IOtpGenerator otpGenerator,
        IOptions<FeatureFlagsOptions> featureFlags)
    {
        _userRepository = userRepository;
        _otpRepository = otpRepository;
        _emailSender = emailSender;
        _otpGenerator = otpGenerator;
        _featureFlags = featureFlags.Value;
    }

    public async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (!_featureFlags.EnableOtp)
            return Result.Failure(Error.FeatureDisabled("OTP registration is disabled."));

        var email = Email.Create(request.Email);
        var name = request.Name.Trim();

        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
            return Result.Failure(Error.Conflict("A user with this email already exists."));

        var user = UserFactory.CreatePending(email, name);
        var code = _otpGenerator.Generate();
        var otp = UserOtp.Create(Guid.NewGuid(), email, code, TimeSpan.FromMinutes(10));

        await _userRepository.AddAsync(user, cancellationToken);
        await _otpRepository.AddAsync(otp, cancellationToken);

        await _emailSender.SendAsync(
            email.ToString(),
            "Your verification code",
            $"Your OTP code is: {code}. It expires in 10 minutes.",
            cancellationToken);

        return Result.Success();
    }
}
