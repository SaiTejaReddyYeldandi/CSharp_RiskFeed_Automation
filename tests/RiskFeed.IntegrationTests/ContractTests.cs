using System.Net;
using System.Text.Json;
using RiskFeed.IntegrationTests.Infrastructure;

namespace RiskFeed.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class ContractTests(PostgresFixture fixture)
{
    [Fact]
    public async Task OpenApiContract_ContainsVersionedFeedOperationsAndResponses()
    {
        using HttpResponseMessage response = await fixture.Client.GetAsync(
            "/swagger/v1/swagger.json",
            TestContext.Current.CancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(
                TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith(
            "3.0.",
            document.RootElement.GetProperty("openapi").GetString());

        JsonElement paths = document.RootElement.GetProperty("paths");
        JsonProperty collectionPath = paths
            .EnumerateObject()
            .Single(path => path.Name.TrimEnd('/') == "/api/v1/risk-feed");
        JsonProperty itemPath = paths
            .EnumerateObject()
            .Single(path => path.Name == "/api/v1/risk-feed/{eventId}");

        Assert.True(collectionPath.Value.TryGetProperty("get", out _));
        JsonElement post = collectionPath.Value.GetProperty("post");
        Assert.True(post.GetProperty("responses").TryGetProperty("201", out _));
        Assert.True(post.GetProperty("responses").TryGetProperty("422", out _));
        Assert.True(post.GetProperty("responses").TryGetProperty("409", out _));
        Assert.True(itemPath.Value.TryGetProperty("get", out _));
    }
}
