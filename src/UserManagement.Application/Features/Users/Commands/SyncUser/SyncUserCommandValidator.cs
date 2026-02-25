using FluentValidation;

namespace UserManagement.Application.Features.Users.Commands.SyncUser;

public sealed class SyncUserCommandValidator : AbstractValidator<SyncUserCommand>
{
    private static readonly System.Text.RegularExpressions.Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    public SyncUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .Must(e => EmailRegex.IsMatch(e?.Trim() ?? "")).WithMessage("Email must be a valid format.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.CognitoSub)
            .NotEmpty().WithMessage("CognitoSub is required.");
    }
}
