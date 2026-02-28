extern alias Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Domain.Repositories;
using UserManagement.IntegrationTests.Stubs;
using Program = Api::UserManagement.API.Program;

namespace UserManagement.IntegrationTests;

/// <summary>
/// WebApplicationFactory that registers stub implementations for Application handlers
/// until Infrastructure implements repositories and external services (Milestone 04).
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IUserRepository, StubUserRepository>();
            services.AddSingleton<IUserOtpRepository, StubUserOtpRepository>();
            services.AddSingleton<IUnitOfWork, StubUnitOfWork>();
            services.AddSingleton<IEmailSender, StubEmailSender>();
            services.AddSingleton<IOtpGenerator, StubOtpGenerator>();
            services.AddSingleton<IDateTimeProvider, StubDateTimeProvider>();
            services.AddSingleton<ICognitoIdentityService, StubCognitoIdentityService>();
        });
    }
}
