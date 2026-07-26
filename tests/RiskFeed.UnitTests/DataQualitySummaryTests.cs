using RiskFeed.Core.Domain;
using RiskFeed.Core.Models;

namespace RiskFeed.UnitTests;

public sealed class DataQualitySummaryTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 26, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Evaluate_WithContiguousFreshEvents_ReturnsPass()
    {
        RiskEvent[] events =
        [
            CreateEvent(1, Now.AddSeconds(-20)),
            CreateEvent(2, Now.AddSeconds(-10)),
            CreateEvent(3, Now.AddSeconds(-5))
        ];

        DataQualitySummary summary = DataQualitySummary.Evaluate(events, Now);

        Assert.Equal(QualityStatus.Pass, summary.Status);
        Assert.Equal(3, summary.RecordCount);
        Assert.Equal(0, summary.SequenceGapCount);
        Assert.Equal(0, summary.DuplicateSequenceCount);
        Assert.Equal(5, summary.LatestRecordAgeSeconds);
    }

    [Fact]
    public void Evaluate_WithGapAndDuplicate_ReturnsFail()
    {
        RiskEvent[] events =
        [
            CreateEvent(1, Now),
            CreateEvent(3, Now),
            CreateEvent(3, Now) with { EventId = Guid.NewGuid() }
        ];

        DataQualitySummary summary = DataQualitySummary.Evaluate(events, Now);

        Assert.Equal(QualityStatus.Fail, summary.Status);
        Assert.Equal(1, summary.SequenceGapCount);
        Assert.Equal(1, summary.DuplicateSequenceCount);
    }

    [Fact]
    public void Evaluate_WithOnlyStaleEvents_ReturnsWarning()
    {
        RiskEvent[] events =
        [
            CreateEvent(1, Now.AddMinutes(-11)),
            CreateEvent(2, Now.AddMinutes(-12))
        ];

        DataQualitySummary summary = DataQualitySummary.Evaluate(events, Now);

        Assert.Equal(QualityStatus.Warning, summary.Status);
        Assert.Equal(2, summary.StaleRecordCount);
    }

    [Fact]
    public void Evaluate_WithNoEvents_ReturnsWarningWithoutAge()
    {
        DataQualitySummary summary = DataQualitySummary.Evaluate([], Now);

        Assert.Equal(QualityStatus.Warning, summary.Status);
        Assert.Null(summary.LatestRecordAgeSeconds);
        Assert.Equal(0, summary.RecordCount);
    }

    private static RiskEvent CreateEvent(long sequence, DateTimeOffset asOfUtc) =>
        new(
            Guid.NewGuid(),
            "PF-ALPHA",
            "EURUSD",
            RiskMetric.ValueAtRisk95,
            100_000m + sequence,
            "USD",
            asOfUtc,
            sequence,
            "risk-engine-v1",
            Now);
}
