using FluentValidation;
using UserManagement.Application.Common.Validators;

namespace UserManagement.Application.Features.Users.Commands.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    private static readonly System.Text.RegularExpressions.Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    public RegisterUserCommandValidator()
    {
        Include(new IdempotentCommandValidator<RegisterUserCommand>());
        
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .Must(e => EmailRegex.IsMatch(e?.Trim() ?? "")).WithMessage("Email must be a valid format.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");
    }
}
