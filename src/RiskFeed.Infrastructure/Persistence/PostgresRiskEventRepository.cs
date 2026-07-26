using System.Text;
using Npgsql;
using NpgsqlTypes;
using RiskFeed.Core.Abstractions;
using RiskFeed.Core.Domain;
using RiskFeed.Core.Models;

namespace RiskFeed.Infrastructure.Persistence;

public sealed class PostgresRiskEventRepository(NpgsqlDataSource dataSource)
    : IRiskEventRepository
{
    public async Task<IReadOnlyList<RiskEvent>> QueryAsync(
        RiskFeedQuery query,
        CancellationToken cancellationToken)
    {
        StringBuilder sql = new(
            """
            SELECT event_id,
                   portfolio_id,
                   instrument,
                   metric,
                   risk_value,
                   currency,
                   as_of_utc,
                   sequence_no,
                   source,
                   ingested_at_utc
              FROM risk_events
             WHERE portfolio_id = @portfolio_id
            """);

        if (query.FromUtc.HasValue)
        {
            sql.AppendLine(" AND as_of_utc >= @from_utc");
        }

        if (query.ToUtc.HasValue)
        {
            sql.AppendLine(" AND as_of_utc <= @to_utc");
        }

        sql.AppendLine(" ORDER BY sequence_no DESC LIMIT @limit;");

        await using NpgsqlCommand command = dataSource.CreateCommand(sql.ToString());
        command.Parameters.AddWithValue(
            "portfolio_id",
            NpgsqlDbType.Varchar,
            query.PortfolioId);
        command.Parameters.AddWithValue("limit", NpgsqlDbType.Integer, query.Limit);

        if (query.FromUtc.HasValue)
        {
            command.Parameters.AddWithValue(
                "from_utc",
                NpgsqlDbType.TimestampTz,
                query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            command.Parameters.AddWithValue(
                "to_utc",
                NpgsqlDbType.TimestampTz,
                query.ToUtc.Value);
        }

        List<RiskEvent> events = [];
        await using NpgsqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            events.Add(Map(reader));
        }

        return events;
    }

    public async Task<RiskEvent?> FindAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT event_id,
                   portfolio_id,
                   instrument,
                   metric,
                   risk_value,
                   currency,
                   as_of_utc,
                   sequence_no,
                   source,
                   ingested_at_utc
              FROM risk_events
             WHERE event_id = @event_id;
            """;

        await using NpgsqlCommand command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("event_id", NpgsqlDbType.Uuid, eventId);

        await using NpgsqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<bool> InsertAsync(
        RiskEvent riskEvent,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            INSERT INTO risk_events (
                event_id,
                portfolio_id,
                instrument,
                metric,
                risk_value,
                currency,
                as_of_utc,
                sequence_no,
                source,
                ingested_at_utc)
            VALUES (
                @event_id,
                @portfolio_id,
                @instrument,
                @metric,
                @risk_value,
                @currency,
                @as_of_utc,
                @sequence_no,
                @source,
                @ingested_at_utc)
            ON CONFLICT DO NOTHING;
            """;

        await using NpgsqlCommand command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue(
            "event_id",
            NpgsqlDbType.Uuid,
            riskEvent.EventId);
        command.Parameters.AddWithValue(
            "portfolio_id",
            NpgsqlDbType.Varchar,
            riskEvent.PortfolioId);
        command.Parameters.AddWithValue(
            "instrument",
            NpgsqlDbType.Varchar,
            riskEvent.Instrument);
        command.Parameters.AddWithValue(
            "metric",
            NpgsqlDbType.Varchar,
            riskEvent.Metric.ToString());
        command.Parameters.AddWithValue(
            "risk_value",
            NpgsqlDbType.Numeric,
            riskEvent.Value);
        command.Parameters.AddWithValue(
            "currency",
            NpgsqlDbType.Char,
            riskEvent.Currency);
        command.Parameters.AddWithValue(
            "as_of_utc",
            NpgsqlDbType.TimestampTz,
            riskEvent.AsOfUtc);
        command.Parameters.AddWithValue(
            "sequence_no",
            NpgsqlDbType.Bigint,
            riskEvent.Sequence);
        command.Parameters.AddWithValue(
            "source",
            NpgsqlDbType.Varchar,
            riskEvent.Source);
        command.Parameters.AddWithValue(
            "ingested_at_utc",
            NpgsqlDbType.TimestampTz,
            riskEvent.IngestedAtUtc);

        int affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows == 1;
    }

    private static RiskEvent Map(NpgsqlDataReader reader) =>
        new(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            Enum.Parse<RiskMetric>(reader.GetString(3), ignoreCase: false),
            reader.GetDecimal(4),
            reader.GetString(5),
            reader.GetFieldValue<DateTimeOffset>(6),
            reader.GetInt64(7),
            reader.GetString(8),
            reader.GetFieldValue<DateTimeOffset>(9));
}
