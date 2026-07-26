using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace RiskFeed.Infrastructure.Persistence;

public sealed class PostgresHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using NpgsqlCommand command =
                dataSource.CreateCommand("SELECT 1;");
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy("PostgreSQL is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "PostgreSQL is not reachable.",
                exception);
        }
    }
}
