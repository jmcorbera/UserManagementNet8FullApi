using MediatR;
using UserManagement.Application.Common.Results;

namespace UserManagement.Application.Features.Users.Commands.VerifyOtp;

public sealed record VerifyOtpCommand(string Email, string Code) : IRequest<Result>;
