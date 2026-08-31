using System.Reflection;

using NetArchTest.Rules;

using Shouldly;

using Xunit;

namespace Cinema.Architecture.Tests;

public sealed class ModuleBoundaryTests
{
    private static readonly string[] ModuleNames =
    [
        "Catalog",
        "Seating",
        "Pricing",
        "Ordering",
        "Payments",
        "Ticketing",
        "Loyalty",
        "Concessions",
        "Identity",
        "Notifications",
    ];

    [Fact]
    public void ModuleDoesNotDependOnAnotherModule()
    {
        foreach (var moduleName in ModuleNames)
        {
            var assembly = Assembly.Load($"Cinema.{moduleName}");
            var forbidden = ModuleNames
                .Where(other => other != moduleName)
                .Select(other => $"Cinema.{other}")
                .ToArray();

            var result = Types.InAssembly(assembly)
                .Should()
                .NotHaveDependencyOnAny(forbidden)
                .GetResult();

            var offenders = string.Join(", ", result.FailingTypeNames ?? []);
            result.IsSuccessful.ShouldBeTrue(
                $"These Cinema.{moduleName} types depend on another module: {offenders}");
        }
    }
}
