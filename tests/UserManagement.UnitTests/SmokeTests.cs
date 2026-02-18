using FluentAssertions;
using Xunit;

namespace UserManagement.UnitTests;

/// <summary>
/// Milestone 01-setup: smoke tests to verify solution and project references load correctly.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void Solution_compiles_and_UnitTests_project_loads()
    {
        true.Should().BeTrue("the UnitTests project loads and Domain reference is resolved");
    }

    [Fact]
    public void Domain_assembly_can_be_loaded()
    {
        var domainAssembly = typeof(UserManagement.Domain.AssemblyReference).Assembly;
        domainAssembly.Should().NotBeNull();
        domainAssembly.GetName().Name.Should().Contain("UserManagement.Domain");
    }
}
