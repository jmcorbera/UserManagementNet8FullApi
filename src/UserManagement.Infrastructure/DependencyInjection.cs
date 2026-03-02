using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleNotificationService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Domain.Repositories;
using UserManagement.Infrastructure.BackgroundServices;
using UserManagement.Infrastructure.Options;
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
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.Configure<OtpGeneratorOptions>(configuration.GetSection(OtpGeneratorOptions.SectionName));

        services.AddScoped<IOtpSettingsProvider, OtpSettingsProvider>();
        services.AddScoped<IOtpGenerator, SecureOtpGenerator>();

        services.AddSingleton<ICognitoIdentityService, MockCognitoIdentityService>();

        services.Configure<SesOptions>(configuration.GetSection(SesOptions.SectionName));
        services.AddScoped<IAmazonSimpleEmailServiceV2>(sp =>
        {
            var sesOptions = configuration.GetSection(SesOptions.SectionName).Get<SesOptions>();
            var region = RegionEndpoint.GetBySystemName(sesOptions?.Region ?? "us-east-1");

            if (!string.IsNullOrWhiteSpace(sesOptions?.AccessKey) && !string.IsNullOrWhiteSpace(sesOptions?.SecretKey))
            {
                var credentials = new BasicAWSCredentials(sesOptions.AccessKey, sesOptions.SecretKey);
                return new AmazonSimpleEmailServiceV2Client(credentials, region);
            }

            return new AmazonSimpleEmailServiceV2Client(region);
        });
        services.AddScoped<IEmailSender, SesEmailSender>();

        services.Configure<SnsOptions>(configuration.GetSection(SnsOptions.SectionName));

        services.AddScoped(sp =>
        {
            var snsOptions = configuration.GetSection(SnsOptions.SectionName).Get<SnsOptions>();
            var region = RegionEndpoint.GetBySystemName(snsOptions?.Region ?? "us-east-1");

            if (!string.IsNullOrWhiteSpace(snsOptions?.AccessKey) && !string.IsNullOrWhiteSpace(snsOptions?.SecretKey))
            {
                var credentials = new BasicAWSCredentials(snsOptions.AccessKey, snsOptions.SecretKey);
                return new AmazonSimpleNotificationServiceClient(credentials, region);
            }

            return new AmazonSimpleNotificationServiceClient(region);
        });

        services.AddScoped<IMessagePublisher, SnsMessagePublisher>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SnsMessagePublisher>>();
            var options = sp.GetRequiredService<IOptions<SnsOptions>>();
            var snsClient = sp.GetRequiredService<AmazonSimpleNotificationServiceClient>();

            return new SnsMessagePublisher(options, logger, snsClient);
        });

        services.Configure<OutboxProcessorOptions>(configuration.GetSection(OutboxProcessorOptions.SectionName));
        services.AddHostedService<OutboxProcessor>();

        services.Configure<CleanupJobOptions>(configuration.GetSection(CleanupJobOptions.SectionName));
        services.AddHostedService<OutboxCleanupJob>();
        services.AddHostedService<IdempotencyCleanupJob>();

        return services;
    }
}
