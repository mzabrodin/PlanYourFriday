namespace PlanYourFriday.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class SharedApi : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
