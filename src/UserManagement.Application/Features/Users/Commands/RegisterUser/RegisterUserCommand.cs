using MediatR;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Application.Common.Results;

namespace UserManagement.Application.Features.Users.Commands.RegisterUser;

public sealed record RegisterUserCommand(string Email, string Name, Guid IdempotencyKey) : IRequest<Result>, IIdempotentCommand;
