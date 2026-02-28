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
    private readonly IUnitOfWork _unitOfWork;
    private readonly FeatureFlagsOptions _featureFlags;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IUserOtpRepository otpRepository,
        IEmailSender emailSender,
        IOtpGenerator otpGenerator,
        IUnitOfWork unitOfWork,
        IOptions<FeatureFlagsOptions> featureFlags)
    {
        _userRepository = userRepository;
        _otpRepository = otpRepository;
        _emailSender = emailSender;
        _otpGenerator = otpGenerator;
        _unitOfWork = unitOfWork;
        _featureFlags = featureFlags.Value;
    }

    public async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);
        var name = request.Name.Trim();

        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
            return Result.Failure(Error.Conflict("A user with this email already exists."));

        var user = UserFactory.CreatePending(email, name);

        var otp = await CreateOtpIfEnabledAsync(user, email, cancellationToken);

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (otp is not null)
            await SendOtpEmailAsync(email, otp, cancellationToken);

        return Result.Success();
    }

    private async Task<UserOtp?> CreateOtpIfEnabledAsync(User user, Email email, CancellationToken cancellationToken)
    {
        if (!_featureFlags.EnableOtp)
            return null;

        var code = _otpGenerator.Generate();
        var otp = UserOtp.Create(Guid.NewGuid(), email, code, TimeSpan.FromMinutes(10));

        user.RaiseRegistrationRequestedEvent(code);
        await _otpRepository.AddAsync(otp, cancellationToken);

        return otp;
    }
    
    private async Task SendOtpEmailAsync(Email email, UserOtp otp, CancellationToken cancellationToken)
    {
        await _emailSender.SendAsync(
            email.ToString(),
            "Your verification code",
            $"Your OTP code is: {otp}. It expires in 10 minutes.",
            cancellationToken);
    }
}
