using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Application.Common.Behaviors;
using UserManagement.Application.Common.Results;
using UserManagement.Application.Features.Users.Commands.RegisterUser;
using Xunit;

namespace UserManagement.Application.UnitTests.Common.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_When_Request_Invalid_Returns_Validation_Error_With_ValidationErrors()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RegisterUserCommand).Assembly));
        services.AddValidatorsFromAssembly(typeof(RegisterUserCommand).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        var provider = services.BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();
        var command = new RegisterUserCommand("", "Name", Guid.NewGuid());
        var result = await mediator.Send(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(Error.Codes.Validation);
        result.Error.ValidationErrors.Should().NotBeNull();
        result.Error.ValidationErrors!.Keys.Should().Contain("Email");
    }
}
