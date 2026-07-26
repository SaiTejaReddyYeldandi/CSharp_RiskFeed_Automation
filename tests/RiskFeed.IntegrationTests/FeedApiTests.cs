using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RiskFeed.Core.Domain;
using RiskFeed.Core.Models;
using RiskFeed.IntegrationTests.Infrastructure;

namespace RiskFeed.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class FeedApiTests(PostgresFixture fixture)
{
    [Fact]
    public async Task GetFeed_WithReadEntitlement_ReturnsOrderedQualityCheckedData()
    {
        using HttpRequestMessage request = PostgresFixture.AuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/risk-feed?portfolioId=PF-ALPHA&limit=5",
            "risk:read");

        using HttpResponseMessage response = await fixture.Client.SendAsync(
            request,
            TestContext.Current.CancellationToken);
        RiskFeedPage? page = await response.Content.ReadFromJsonAsync<RiskFeedPage>(
            TestJson.Options,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(page);
        Assert.Equal(5, page.Items.Count);
        Assert.Equal(
            page.Items.OrderByDescending(item => item.Sequence),
            page.Items);
        Assert.Equal(QualityStatus.Pass, page.DataQuality.Status);
        Assert.Equal(0, page.DataQuality.SequenceGapCount);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }

    [Fact]
    public async Task GetFeed_WithoutClientIdentity_ReturnsUnauthorizedProblem()
    {
        using HttpResponseMessage response = await fixture.Client.GetAsync(
            "/api/v1/risk-feed?portfolioId=PF-ALPHA",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetFeed_WithoutRequiredEntitlement_ReturnsForbidden()
    {
        using HttpRequestMessage request = PostgresFixture.AuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/risk-feed?portfolioId=PF-ALPHA",
            "profile:read");

        using HttpResponseMessage response = await fixture.Client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetFeed_WithInvalidLimit_ReturnsBadRequest()
    {
        using HttpRequestMessage request = PostgresFixture.AuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/risk-feed?portfolioId=PF-ALPHA&limit=501",
            "risk:read");

        using HttpResponseMessage response = await fixture.Client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_WithInvalidData_ReturnsAllQualityViolations()
    {
        IngestRiskEventRequest payload = new(
            Guid.NewGuid(),
            "pf-invalid",
            "BAD INSTRUMENT",
            RiskMetric.ValueAtRisk95,
            -10m,
            "US",
            DateTimeOffset.UtcNow.AddMinutes(5),
            0,
            "INVALID SOURCE");
        using HttpRequestMessage request = PostgresFixture.AuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/risk-feed",
            "risk:write");
        request.Content = JsonContent.Create(payload, options: TestJson.Options);

        using HttpResponseMessage response = await fixture.Client.SendAsync(
            request,
            TestContext.Current.CancellationToken);
        using JsonDocument problem = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(
                TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        JsonElement errors = problem.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("portfolioId", out _));
        Assert.True(errors.TryGetProperty("instrument", out _));
        Assert.True(errors.TryGetProperty("currency", out _));
        Assert.True(errors.TryGetProperty("sequence", out _));
        Assert.True(errors.TryGetProperty("asOfUtc", out _));
    }

    [Fact]
    public async Task Ingest_ThenRead_AndRepeat_ReturnsCreatedThenConflict()
    {
        Guid eventId = Guid.Parse("9e8a750e-8d5c-4e23-8a9d-04a32933271f");
        IngestRiskEventRequest payload = new(
            eventId,
            "PF-GAMMA",
            "GBPUSD",
            RiskMetric.ExpectedShortfall,
            52_120.25m,
            "USD",
            DateTimeOffset.UtcNow.AddSeconds(-5),
            3001,
            "risk-engine-v1");

        using HttpResponseMessage created = await SendIngestAsync(payload);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(
            $"/api/v1/risk-feed/{eventId}",
            created.Headers.Location?.OriginalString);

        using HttpRequestMessage getRequest = PostgresFixture.AuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/risk-feed/{eventId}",
            "risk:read");
        using HttpResponseMessage read = await fixture.Client.SendAsync(
            getRequest,
            TestContext.Current.CancellationToken);
        RiskEvent? stored = await read.Content.ReadFromJsonAsync<RiskEvent>(
            TestJson.Options,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.NotNull(stored);
        Assert.Equal(payload.Value, stored.Value);
        Assert.Equal(payload.Sequence, stored.Sequence);

        using HttpResponseMessage duplicate = await SendIngestAsync(payload);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    private async Task<HttpResponseMessage> SendIngestAsync(
        IngestRiskEventRequest payload)
    {
        using HttpRequestMessage request = PostgresFixture.AuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/risk-feed",
            "risk:write");
        request.Content = JsonContent.Create(payload, options: TestJson.Options);
        return await fixture.Client.SendAsync(
            request,
            TestContext.Current.CancellationToken);
    }
}
