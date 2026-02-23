using UserManagement.Domain.Entities;
using UserManagement.Domain.Enums;

namespace UserManagement.Domain.Specifications;

/// <summary>
/// Specification para filtrar usuarios activos.
/// </summary>
public sealed class ActiveUsersSpec : ISpecification<User>
{
    public bool IsSatisfiedBy(User user)
    {
        return user.Status == UserStatus.Active && !user.IsDeleted;
    }
}
