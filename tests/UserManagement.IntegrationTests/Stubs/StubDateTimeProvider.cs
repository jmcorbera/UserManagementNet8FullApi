using UserManagement.Application.Common.Abstractions;

namespace UserManagement.IntegrationTests.Stubs;

public sealed class StubDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
