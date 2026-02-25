using UserManagement.Application.Common.Abstractions;

namespace UserManagement.Application.UnitTests.Fakes;

public sealed class FakeOtpGenerator : IOtpGenerator
{
    public string NextCode { get; set; } = "123456";

    public string Generate() => NextCode;
}
