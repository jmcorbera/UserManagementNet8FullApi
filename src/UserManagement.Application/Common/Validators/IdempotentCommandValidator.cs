using FluentValidation;
using UserManagement.Application.Common.Abstractions;

namespace UserManagement.Application.Common.Validators;

public class IdempotentCommandValidator<TCommand> : AbstractValidator<TCommand> where TCommand : IIdempotentCommand
{
    public IdempotentCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("IdempotencyKey is required.")
            .Must(guid => guid != Guid.Empty)
            .WithMessage("IdempotencyKey must be a valid GUID.");
    }
}