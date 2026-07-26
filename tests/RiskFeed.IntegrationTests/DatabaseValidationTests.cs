using System.Net.Http.Json;
using Npgsql;
using RiskFeed.Core.Models;
using RiskFeed.IntegrationTests.Infrastructure;

namespace RiskFeed.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class DatabaseValidationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task ApiLatestEvent_MatchesPostgresSourceOfTruth()
    {
        using HttpRequestMessage request = PostgresFixture.AuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/risk-feed?portfolioId=PF-BETA&limit=1",
            "risk:read");
        using HttpResponseMessage response = await fixture.Client.SendAsync(
            request,
            TestContext.Current.CancellationToken);
        RiskFeedPage? page = await response.Content.ReadFromJsonAsync<RiskFeedPage>(
            TestJson.Options,
            TestContext.Current.CancellationToken);

        await using NpgsqlConnection connection =
            new(fixture.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlCommand command = new(
            """
            SELECT event_id, risk_value, sequence_no
              FROM risk_events
             WHERE portfolio_id = 'PF-BETA'
             ORDER BY sequence_no DESC
             LIMIT 1;
            """,
            connection);
        await using NpgsqlDataReader reader =
            await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        Assert.True(await reader.ReadAsync(TestContext.Current.CancellationToken));

        Assert.NotNull(page);
        Assert.Equal(reader.GetGuid(0), page.Items[0].EventId);
        Assert.Equal(reader.GetDecimal(1), page.Items[0].Value);
        Assert.Equal(reader.GetInt64(2), page.Items[0].Sequence);
    }

    [Fact]
    public async Task SeededData_SatisfiesDatabaseQualityConstraints()
    {
        await using NpgsqlConnection connection =
            new(fixture.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlCommand command = new(
            """
            SELECT COUNT(*)
              FROM risk_events
             WHERE portfolio_id IS NULL
                OR currency !~ '^[A-Z]{3}$'
                OR sequence_no <= 0
                OR ABS(risk_value) > 1000000000
                OR as_of_utc > CURRENT_TIMESTAMP + INTERVAL '2 minutes';
            """,
            connection);

        object? result = await command.ExecuteScalarAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(0L, result);
    }

    [Fact]
    public async Task DuplicatePortfolioSequence_IsRejectedByDatabase()
    {
        await using NpgsqlConnection connection =
            new(fixture.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlTransaction transaction =
            await connection.BeginTransactionAsync(
                TestContext.Current.CancellationToken);
        await using NpgsqlCommand command = new(
            """
            INSERT INTO risk_events (
                event_id, portfolio_id, instrument, metric, risk_value,
                currency, as_of_utc, sequence_no, source, ingested_at_utc)
            VALUES (
                @event_id, 'PF-ALPHA', 'EURUSD', 'ValueAtRisk95', 1,
                'USD', CURRENT_TIMESTAMP, 1010, 'integration-test',
                CURRENT_TIMESTAMP);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("event_id", Guid.NewGuid());

        PostgresException exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync(
                TestContext.Current.CancellationToken));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
        await transaction.RollbackAsync(TestContext.Current.CancellationToken);
    }
}
