using UserManagement.Application.Common.Abstractions;

namespace UserManagement.IntegrationTests.Stubs;

public sealed class StubOtpGenerator : IOtpGenerator
{
    public string Generate() => "000000";
}
