using MediatR;
using UserManagement.Application.Common.Pagination;
using UserManagement.Application.Common.Results;
using UserManagement.Application.Features.Users.Models;
using UserManagement.Domain.Enums;

namespace UserManagement.Application.Features.Users.Queries.GetUsers;

public sealed record GetUsersQuery(
    int PageNumber = 1,
    int PageSize = 10,
    UserStatus? StatusFilter = null,
    bool IncludeDeleted = false) : IRequest<Result<PagedResult<UserResponse>>>;
