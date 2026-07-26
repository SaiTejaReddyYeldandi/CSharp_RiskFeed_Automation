namespace RiskFeed.UiTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class BrowserCollection : ICollectionFixture<BrowserFixture>
{
    public const string Name = "playwright-browser";
}
