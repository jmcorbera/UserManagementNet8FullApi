using MediatR;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Application.Common.Results;

namespace UserManagement.Application.Features.Users.Commands.VerifyOtp;

public sealed record VerifyOtpCommand(string Email, string Code, Guid IdempotencyKey) : IRequest<Result>, IIdempotentCommand;
