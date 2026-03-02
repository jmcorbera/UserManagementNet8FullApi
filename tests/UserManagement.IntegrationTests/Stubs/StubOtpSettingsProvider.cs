using UserManagement.Application.Common.Abstractions;

namespace UserManagement.IntegrationTests.Stubs;

public sealed class StubOtpSettingsProvider : IOtpSettingsProvider
{
    public int Length => 6;
    public int ValidForMinutes => 10;
}
