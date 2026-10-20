using System.Reflection;

using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.UnitTests;

public class ArchitectureTests
{
    [Fact]
    public void Domain_DoesNotReferenceOtherProjects()
    {
        Assert.Empty(ProjectReferences(typeof(Entity).Assembly));
    }

    [Theory]
    [InlineData("PlanYourFriday.Infrastructure")]
    [InlineData("PlanYourFriday.Api")]
    [InlineData("PlanYourFriday.Worker")]
    public void Application_DoesNotReferenceOuterLayers(string outerLayer)
    {
        Assert.DoesNotContain(outerLayer, ProjectReferences(typeof(OutboxMessage).Assembly));
    }

    private static string[] ProjectReferences(Assembly assembly)
    {
        return
        [
            .. assembly.GetReferencedAssemblies()
                .Select(a => a.Name!)
                .Where(name => name.StartsWith("PlanYourFriday.", StringComparison.Ordinal))];
    }
}
