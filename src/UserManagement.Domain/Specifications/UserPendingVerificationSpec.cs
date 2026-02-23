using UserManagement.Domain.Entities;
using UserManagement.Domain.Enums;
using UserManagement.Domain.ValueObjects;

namespace UserManagement.Domain.Specifications;

/// <summary>
/// Specification para buscar usuarios pendientes de verificación por email.
/// </summary>
public sealed class UserPendingVerificationSpec : ISpecification<User>
{
    private readonly Email _email;

    public UserPendingVerificationSpec(Email email)
    {
        _email = email;
    }

    public bool IsSatisfiedBy(User user)
    {
        return user.Email.Equals(_email) 
            && user.Status == UserStatus.PendingVerification 
            && !user.IsDeleted;
    }
}
