using MediatR;
using UserManagement.Application.Common.Results;

namespace UserManagement.Application.Features.Users.Commands.SyncUser;

public sealed record SyncUserCommand(string Email, string Name, string CognitoSub) : IRequest<Result>;
