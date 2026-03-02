using Microsoft.Extensions.Options;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Infrastructure.Options;

namespace UserManagement.Infrastructure.Services;

public sealed class OtpSettingsProvider : IOtpSettingsProvider
{
    private readonly OtpGeneratorOptions _options;

    public OtpSettingsProvider(IOptions<OtpGeneratorOptions> options)
    {
        _options = options.Value;
    }

    public int Length => _options.Length;
    public int ValidForMinutes => _options.ValidForMinutes;
}