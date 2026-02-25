using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Application.Common.Behaviors;
using UserManagement.Application.Common.Options;
using UserManagement.Application.Fakes.FakeInstances;
using UserManagement.Domain.Repositories;


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

        // fake instances for development
        // Real implementation will be added later in infrastructure layer
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserOtpRepository, UserOtpRepository>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IOtpGenerator, OtpGenerator>();
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ICognitoIdentityService, CognitoIdentityService>();

        return services;
    }
}
