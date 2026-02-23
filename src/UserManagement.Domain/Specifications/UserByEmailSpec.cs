using UserManagement.Domain.Entities;
using UserManagement.Domain.ValueObjects;

namespace UserManagement.Domain.Specifications;

/// <summary>
/// Specification para buscar un usuario por email.
/// </summary>
public sealed class UserByEmailSpec : ISpecification<User>
{
    private readonly Email _email;

    public UserByEmailSpec(Email email)
    {
        _email = email;
    }

    public bool IsSatisfiedBy(User user)
    {
        return user.Email.Equals(_email) && !user.IsDeleted;
    }
}
