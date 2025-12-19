using NetArchTest.Rules;
using Xunit;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Infrastructure.Persistence.Repositories;
using System.Reflection;

namespace BotCarniceria.ArchitectureTests;

public class ArchitectureTests
{
    private const string DomainNamespace = "BotCarniceria.Core.Domain";
    private const string ApplicationNamespace = "BotCarniceria.Core.Application";
    private const string InfrastructureNamespace = "BotCarniceria.Infrastructure";
    private const string PresentationNamespace = "BotCarniceria.Presentation";

    private static Assembly GetCoreAssembly() => typeof(Cliente).Assembly;
    private static Assembly GetInfrastructureAssembly() => typeof(ConfiguracionRepository).Assembly;

    #region Layer Dependency Tests

    [Fact]
    public void Domain_ShouldNotHaveDependencyOnApplication()
    {
        // Arrange
        var domainAssembly = GetCoreAssembly();

        // Act
        var result = Types.InAssembly(domainAssembly)
            .That()
            .ResideInNamespace(DomainNamespace)
            .ShouldNot()
            .HaveDependencyOn(ApplicationNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            $"Domain layer should not depend on Application layer. Violations: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void Domain_ShouldNotHaveDependencyOnInfrastructure()
    {
        // Arrange
        var domainAssembly = GetCoreAssembly();

        // Act
        var result = Types.InAssembly(domainAssembly)
            .That()
            .ResideInNamespace(DomainNamespace)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Domain layer should not depend on Infrastructure layer. Violations: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void Application_ShouldNotHaveDependencyOnInfrastructure()
    {
        // Arrange
        var applicationAssembly = GetCoreAssembly();

        // Act
        var result = Types.InAssembly(applicationAssembly)
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Application layer should not depend on Infrastructure layer. Violations: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    #endregion

    #region Naming Convention Tests

    [Fact]
    public void Interfaces_ShouldStartWithI()
    {
        // Arrange
        var coreAssembly = GetCoreAssembly();

        // Act
        var result = Types.InAssembly(coreAssembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"All interfaces should start with 'I'. Violations: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void Repositories_ShouldHaveRepositorySuffix()
    {
        // Arrange
        var infrastructureAssembly = GetInfrastructureAssembly();

        // Act
        var result = Types.InAssembly(infrastructureAssembly)
            .That()
            .ResideInNamespace($"{InfrastructureNamespace}.Persistence.Repositories")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"All repository classes should end with 'Repository'. Violations: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    #endregion

    #region Domain Model Tests

    [Fact]
    public void Entities_ShouldInheritFromBaseEntity()
    {
        // Arrange
        var domainAssembly = GetCoreAssembly();

        var baseEntityType = domainAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "BaseEntity");

        if (baseEntityType == null)
        {
             // Fallback or explicit check if BaseEntity is named differently or in shared
            Assert.True(false, "BaseEntity not found");
            return;
        }

        // Act
        var result = Types.InAssembly(domainAssembly)
            .That()
            .ResideInNamespace($"{DomainNamespace}.Entities")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .Inherit(baseEntityType)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"All entities should inherit from BaseEntity. Violations: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void DomainEvents_ShouldHaveEventSuffix()
    {
        // Arrange
        var domainAssembly = GetCoreAssembly();

        // Act
        var result = Types.InAssembly(domainAssembly)
            .That()
            .ResideInNamespace($"{DomainNamespace}.Events")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Event")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"All domain events should end with 'Event'. Violations: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void DomainExceptions_ShouldInheritFromException()
    {
        // Arrange
        var domainAssembly = GetCoreAssembly();

        // Act
        var result = Types.InAssembly(domainAssembly)
            .That()
            .ResideInNamespace($"{DomainNamespace}.Exceptions")
            .And()
            .AreClasses()
            .Should()
            .Inherit(typeof(Exception))
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"All domain exceptions should inherit from Exception. Violations: {string.Join(", ", result.FailingTypeNames ?? new List<string>())}");
    }

    #endregion

    #region CQRS Pattern Tests

    [Fact]
    public void Commands_ShouldImplementIRequest()
    {
        // Arrange
        var coreAssembly = GetCoreAssembly();

        // Act
        var commandTypes = Types.InAssembly(coreAssembly)
            .That()
            .ResideInNamespace($"{ApplicationNamespace}.CQRS.Commands")
            .And()
            .AreClasses()
            .GetTypes();

        // Assert - Check if commands implement IRequest
        foreach (var commandType in commandTypes)
        {
            var implementsIRequest = commandType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name == "IRequest`1");
            
            Assert.True(implementsIRequest, 
                $"Command {commandType.Name} should implement IRequest<TResponse>");
        }
    }

    [Fact]
    public void Queries_ShouldImplementIRequest()
    {
        // Arrange
        var coreAssembly = GetCoreAssembly();

        // Act
        var queryTypes = Types.InAssembly(coreAssembly)
            .That()
            .ResideInNamespace($"{ApplicationNamespace}.CQRS.Queries")
            .And()
            .AreClasses()
            .GetTypes();

        // Assert - Check if queries implement IRequest
        foreach (var queryType in queryTypes)
        {
            var implementsIRequest = queryType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name == "IRequest`1");
            
            Assert.True(implementsIRequest, 
                $"Query {queryType.Name} should implement IRequest<TResponse>");
        }
    }

    #endregion
}
