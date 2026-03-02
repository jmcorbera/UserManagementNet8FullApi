using Microsoft.Extensions.Options;
using OtpNet;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Infrastructure.Options;

namespace UserManagement.Infrastructure.Services;

public sealed class SecureOtpGenerator : IOtpGenerator
{
    private readonly OtpGeneratorOptions _options;

    public SecureOtpGenerator(IOptions<OtpGeneratorOptions> options)
    {
        _options = options.Value;
    }

    public string Generate()
    {
        var length = _options.Length;
        var key = KeyGeneration.GenerateRandomKey(20);
        var totp = new Totp(key, step: 30, mode: OtpHashMode.Sha256, totpSize: length);
        
        return totp.ComputeTotp();
    }
}
