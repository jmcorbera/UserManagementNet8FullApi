using MediatR;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Application.Common.Results;
using UserManagement.Domain.Factories;
using UserManagement.Domain.Repositories;
using UserManagement.Domain.ValueObjects;

namespace UserManagement.Application.Features.Users.Commands.SyncUser;

public sealed class SyncUserCommandHandler : IRequestHandler<SyncUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SyncUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SyncUserCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);
        var name = request.Name.Trim();
        var cognitoSub = request.CognitoSub.Trim();

        var existingByCognito = await _userRepository.GetByCognitoSubAsync(cognitoSub, cancellationToken);
        var existingByEmail = await _userRepository.GetByEmailAsync(email, cancellationToken);

        // Inconsistent case
        if (existingByCognito != null &&
            existingByEmail != null &&
            existingByCognito.Id != existingByEmail.Id)
        {
            return Result.Failure(Error.Conflict("Identity mismatch."));
        }

        var user = existingByCognito ?? existingByEmail;

        if (user != null)
        {
            if (user.CognitoSub == null)
                user.SetCognitoSub(cognitoSub);

            user.UpdateName(name);

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        var newUser = UserFactory.FromCognito(email, name, cognitoSub);
        await _userRepository.AddAsync(newUser, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
