using System.Reflection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace RiskFeed.Infrastructure.Persistence;

public sealed partial class DatabaseInitializer(
    NpgsqlDataSource dataSource,
    ILogger<DatabaseInitializer> logger)
{
    private const int MaximumAttempts = 8;
    private const string MigrationResource =
        "RiskFeed.Infrastructure.Migrations.001_initial.sql";

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        string migration = await ReadMigrationAsync(cancellationToken);

        for (int attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            try
            {
                await using NpgsqlCommand command = dataSource.CreateCommand(migration);
                await command.ExecuteNonQueryAsync(cancellationToken);
                LogDatabaseInitialized(logger, attempt);
                return;
            }
            catch (NpgsqlException exception) when (attempt < MaximumAttempts)
            {
                TimeSpan delay = TimeSpan.FromSeconds(Math.Min(attempt * 2, 10));
                LogDatabaseRetry(logger, attempt, delay, exception);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private static async Task<string> ReadMigrationAsync(
        CancellationToken cancellationToken)
    {
        Assembly assembly = typeof(DatabaseInitializer).Assembly;
        await using Stream stream = assembly.GetManifestResourceStream(MigrationResource)
            ?? throw new InvalidOperationException(
                $"Embedded migration '{MigrationResource}' was not found.");
        using StreamReader reader = new(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Risk feed database initialized on attempt {Attempt}.")]
    private static partial void LogDatabaseInitialized(
        ILogger logger,
        int attempt);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Database initialization attempt {Attempt} failed; retrying in {Delay}.")]
    private static partial void LogDatabaseRetry(
        ILogger logger,
        int attempt,
        TimeSpan delay,
        Exception exception);
}
