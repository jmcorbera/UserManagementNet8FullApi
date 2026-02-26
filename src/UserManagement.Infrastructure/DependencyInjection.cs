using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Domain.Repositories;
using UserManagement.Infrastructure.Persistence;
using UserManagement.Infrastructure.Persistence.Repositories;
using UserManagement.Infrastructure.Services;

namespace UserManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MySqlServerConnectionString");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        services.AddDbContext<MySqlDbContext>(options =>
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), o => o.SchemaBehavior(MySqlSchemaBehavior.Ignore));
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserOtpRepository, UserOtpRepository>();

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }
}
