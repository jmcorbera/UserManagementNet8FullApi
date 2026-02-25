using MediatR;
using UserManagement.Application.Common.Pagination;
using UserManagement.Application.Common.Results;
using UserManagement.Application.Features.Users.Models;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Repositories;
using UserManagement.Domain.Specifications;

namespace UserManagement.Application.Features.Users.Queries.GetUsers;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<PagedResult<UserResponse>>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<PagedResult<UserResponse>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var spec = new UsersPaginatedSpec(request.StatusFilter, request.IncludeDeleted);
        var all = await _userRepository.GetAllAsync(cancellationToken);
        var filtered = all.Where(spec.IsSatisfiedBy).ToList();
        var totalCount = filtered.Count;

        var pageNumber = request.PageNumber;
        var pageSize = request.PageSize;
        var items = filtered
            .OrderByDescending(u => u.Created)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(Map)
            .ToList();

        var paged = new PagedResult<UserResponse>(items, pageNumber, pageSize, totalCount);
        return Result<PagedResult<UserResponse>>.Success(paged);
    }

    private static UserResponse Map(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email.ToString(),
            Name = user.Name,
            Status = user.Status,
            CognitoSub = user.CognitoSub,
            Created = user.Created,
            LastModified = user.LastModified,
            IsDeleted = user.IsDeleted
        };
    }
}
