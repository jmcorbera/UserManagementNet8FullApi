using UserManagement.Application.Common.Abstractions;

namespace UserManagement.Application.UnitTests.Fakes;

public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
}
