using UserManagement.Domain.Entities;
using UserManagement.Domain.Enums;

namespace UserManagement.Domain.Specifications;

/// <summary>
/// Specification para paginación de usuarios con filtros opcionales.
/// </summary>
public sealed class UsersPaginatedSpec : ISpecification<User>
{
    private readonly UserStatus? _statusFilter;
    private readonly bool _includeDeleted;

    public UsersPaginatedSpec(UserStatus? statusFilter = null, bool includeDeleted = false)
    {
        _statusFilter = statusFilter;
        _includeDeleted = includeDeleted;
    }

    public bool IsSatisfiedBy(User user)
    {
        if (!_includeDeleted && user.IsDeleted)
        {
            return false;
        }

        if (_statusFilter.HasValue && user.Status != _statusFilter.Value)
        {
            return false;
        }

        return true;
    }
}
