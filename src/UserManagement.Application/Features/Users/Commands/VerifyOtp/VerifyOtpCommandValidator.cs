using FluentValidation;
using UserManagement.Application.Common.Validators;

namespace UserManagement.Application.Features.Users.Commands.VerifyOtp;

public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    private static readonly System.Text.RegularExpressions.Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    public VerifyOtpCommandValidator()
    {
        Include(new IdempotentCommandValidator<VerifyOtpCommand>());
        
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .Must(e => EmailRegex.IsMatch(e?.Trim() ?? "")).WithMessage("Email must be a valid format.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.");
    }
}
