using MediatR;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Application.Common.Results;
using UserManagement.Domain.Common;
using UserManagement.Domain.Enums;
using UserManagement.Domain.Repositories;
using UserManagement.Domain.ValueObjects;

namespace UserManagement.Application.Features.Users.Commands.VerifyOtp;

public sealed class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserOtpRepository _otpRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICognitoIdentityService _cognitoService;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyOtpCommandHandler(
        IUserRepository userRepository,
        IUserOtpRepository otpRepository,
        IDateTimeProvider dateTimeProvider,
        ICognitoIdentityService cognitoService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _otpRepository = otpRepository;
        _dateTimeProvider = dateTimeProvider;
        _cognitoService = cognitoService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);
        var code = request.Code.Trim();
        var now = _dateTimeProvider.UtcNow;

        var otp = await _otpRepository.GetByEmailAndCodeAsync(email, code, cancellationToken);

        if (otp == null)
            return Result.Failure(Error.OtpInvalid("Invalid or expired code."));

        if (!otp.IsValid(now))
            return Result.Failure(Error.OtpExpired("The verification code has expired."));

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user == null || user.IsDeleted)
            return Result.Failure(Error.NotFound("User not found."));


        if (user.Status == UserStatus.Active && user.CognitoSub != null)
            return Result.Success();

        string cognitoSub;

        if (user.CognitoSub == null)
        {
            try
            {
                cognitoSub = await _cognitoService.CreateUserAsync(
                    email.ToString(),
                    user.Name,
                    cancellationToken);
            }
            catch
            {
                return Result.Failure(Error.ExternalService("Identity provider failed."));
            }
        }
        else
        {
            cognitoSub = user.CognitoSub;
        }

        try
        {
            otp.MarkAsUsed();
        }
        catch (DomainException)
        {
            return Result.Failure(Error.OtpInvalid("The code has already been used."));
        }

        if (user.CognitoSub == null)
            user.SetCognitoSub(cognitoSub);

        user.ActivateAndRaiseEvent(cognitoSub);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _otpRepository.UpdateAsync(otp, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
