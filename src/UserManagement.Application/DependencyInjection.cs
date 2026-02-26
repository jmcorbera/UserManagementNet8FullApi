using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Application.Common.Behaviors;
using UserManagement.Application.Common.Options;
using UserManagement.Application.Fakes.FakeInstances;

namespace UserManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.Configure<FeatureFlagsOptions>(configuration.GetSection(FeatureFlagsOptions.SectionName));

        services.AddTransient<IEmailSender, EmailSender>();
        services.AddTransient<IOtpGenerator, OtpGenerator>();
        services.AddTransient<ICognitoIdentityService, CognitoIdentityService>();

        return services;
    }
}
