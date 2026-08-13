using System.Reflection;

using NetArchTest.Rules;

namespace CleanTemplate.ArchitectureTests;

public class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = Assembly.Load("CleanTemplate.Domain");
    private static readonly Assembly ContractsAssembly = Assembly.Load("CleanTemplate.Contracts");
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOn_OuterLayers()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("CleanTemplate.Application", "CleanTemplate.Infrastructure", "CleanTemplate.Api")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Contracts_ShouldNotDependOn_OtherLayers()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("CleanTemplate.Domain", "CleanTemplate.Application", "CleanTemplate.Infrastructure", "CleanTemplate.Api")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_ShouldNotDependOn_OuterLayers()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("CleanTemplate.Infrastructure", "CleanTemplate.Api")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("CleanTemplate.Api")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}