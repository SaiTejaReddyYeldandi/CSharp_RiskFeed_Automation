using RiskFeed.Core.Domain;

namespace RiskFeed.Core.Models;

public enum QualityStatus
{
    Pass,
    Warning,
    Fail
}

public sealed record DataQualitySummary(
    QualityStatus Status,
    int RecordCount,
    int SequenceGapCount,
    int DuplicateSequenceCount,
    int StaleRecordCount,
    double? LatestRecordAgeSeconds)
{
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(10);

    public static DataQualitySummary Evaluate(
        IReadOnlyCollection<RiskEvent> events,
        DateTimeOffset nowUtc)
    {
        if (events.Count == 0)
        {
            return new(
                QualityStatus.Warning,
                0,
                0,
                0,
                0,
                null);
        }

        int gapCount = events
            .GroupBy(item => item.PortfolioId, StringComparer.Ordinal)
            .Sum(group =>
            {
                long[] sequences = group
                    .Select(item => item.Sequence)
                    .Distinct()
                    .Order()
                    .ToArray();

                return sequences
                    .Zip(sequences.Skip(1))
                    .Count(pair => pair.Second - pair.First > 1);
            });

        int duplicates = events
            .GroupBy(item => new { item.PortfolioId, item.Sequence })
            .Count(group => group.Count() > 1);

        int staleRecords = events.Count(item => nowUtc - item.AsOfUtc > StaleAfter);
        DateTimeOffset latest = events.Max(item => item.AsOfUtc);
        double ageSeconds = Math.Max(0, (nowUtc - latest).TotalSeconds);

        QualityStatus status = duplicates > 0 || gapCount > 0
            ? QualityStatus.Fail
            : staleRecords == events.Count
                ? QualityStatus.Warning
                : QualityStatus.Pass;

        return new(
            status,
            events.Count,
            gapCount,
            duplicates,
            staleRecords,
            Math.Round(ageSeconds, 1));
    }
}
