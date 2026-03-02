using UserManagement.Application.Common.Abstractions;

namespace UserManagement.Application.UnitTests.Fakes;

public sealed class FakeOtpSettingsProvider : IOtpSettingsProvider
{
    public int Length { get; set; } = 6;
    public int ValidForMinutes { get; set; } = 10;
}
