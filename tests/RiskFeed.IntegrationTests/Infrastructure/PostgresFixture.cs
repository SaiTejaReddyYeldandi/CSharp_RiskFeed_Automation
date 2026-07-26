using Testcontainers.PostgreSql;

namespace RiskFeed.IntegrationTests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("riskfeed_tests")
        .WithUsername("riskfeed")
        .WithPassword("riskfeed_tests")
        .WithPortBinding(5432, assignRandomHostPort: true)
        .Build();

    public HttpClient Client { get; private set; } = null!;

    public RiskFeedApplicationFactory Factory { get; private set; } = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        Factory = new RiskFeedApplicationFactory(ConnectionString);
        Client = Factory.CreateClient(
            new()
            {
                BaseAddress = new Uri("http://risk-feed.test")
            });
    }

    public async ValueTask DisposeAsync()
    {
        if (Client is not null)
        {
            Client.Dispose();
        }

        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    public static HttpRequestMessage AuthorizedRequest(
        HttpMethod method,
        string path,
        string entitlement)
    {
        HttpRequestMessage request = new(method, path);
        request.Headers.Add("X-Client-Id", "integration-test-client");
        request.Headers.Add("X-Entitlements", entitlement);
        request.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString("D"));
        return request;
    }
}
