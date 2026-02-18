extern alias Api;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace UserManagement.IntegrationTests;

public class ArquitectureTests
{
    private const string UserManagementApplication = "UserManagement.Application";
    private const string UserManagementInfrastructure = "UserManagement.Infrastructure";
    private const string UserManagementAPI = "UserManagement.API";

    [Fact]
    public void domain_Should_not_HaveDependenciesOnOtherProjects()
    {
        // Arrange
        var domainAssembly = typeof(UserManagement.Domain.AssemblyReference).Assembly;

        var otherProjects = new[]
        {
            UserManagementApplication,
            UserManagementInfrastructure,
            UserManagementAPI
        };

        // Act
        var result = Types
            .InAssembly(domainAssembly)
            .ShouldNot().HaveDependencyOnAny(otherProjects)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Domain project should not have dependencies on Application, Infrastructure or API projects.");
    }

    [Fact]
    public void application_Should_not_HaveDependenciesOnOtherProjects()
    {
        // Arrange
        var applicationAssembly = typeof(UserManagement.Application.AssemblyReference).Assembly;

        var otherProjects = new[]
        {
            UserManagementInfrastructure,
            UserManagementAPI
        };

        // Act
        var result = Types
            .InAssembly(applicationAssembly)
            .ShouldNot().HaveDependencyOnAny(otherProjects)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Application project should not depend on Infrastructure or API projects.");
    }

    [Fact]
    public void infrastructure_Should_not_HaveDependenciesOnOtherProjects()
    {
        // Arrange
        var infrastructureAssembly = typeof(UserManagement.Infrastructure.AssemblyReference).Assembly;

        var otherProjects = new[]
        {
            UserManagementAPI
        };

        // Act
        var result = Types
            .InAssembly(infrastructureAssembly)
            .ShouldNot().HaveDependencyOnAny(otherProjects)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Infrastructure project should not have dependencies on API projects.");
    }

    [Fact]
    public void api_Should_only_depend_on_Infrastructure_in_CompositionRoot()
    {
        // Arrange
        var apiAssembly = typeof(Api::UserManagement.API.AssemblyReference).Assembly;

        var otherProjects = new[]
        {
            UserManagementInfrastructure
        };

        // Act
        var result = Types
            .InAssembly(apiAssembly)
            .That()
            .DoNotHaveName("Program", "DependencyInjection", "AssemblyReference")
            .ShouldNot().HaveDependencyOnAny(otherProjects)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("API must only reference Infrastructure from the composition root (Program/DependencyInjection).");
    }
}