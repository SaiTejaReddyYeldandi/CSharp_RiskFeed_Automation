using RiskFeed.Core.Domain;
using RiskFeed.Core.Models;
using RiskFeed.Core.Services;
using RiskFeed.UnitTests.TestDoubles;

namespace RiskFeed.UnitTests;

public sealed class RiskFeedServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 26, 10, 30, 0, TimeSpan.Zero);
    private readonly FixedTimeProvider _timeProvider = new(Now);

    [Fact]
    public async Task GetFeedAsync_UsesRepositoryDataAndCalculatesQuality()
    {
        InMemoryRiskEventRepository repository = new(
        [
            CreateEvent(1001),
            CreateEvent(1002)
        ]);
        RiskFeedService service = CreateService(repository);

        RiskFeedPage page = await service.GetFeedAsync(
            new("PF-ALPHA", Limit: 10),
            TestContext.Current.CancellationToken);

        Assert.Equal([1002L, 1001L], page.Items.Select(item => item.Sequence));
        Assert.Equal(QualityStatus.Pass, page.DataQuality.Status);
        Assert.Equal(Now, page.GeneratedAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public async Task GetFeedAsync_WithInvalidLimit_Throws(int limit)
    {
        RiskFeedService service = CreateService(new());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetFeedAsync(
                new("PF-ALPHA", Limit: limit),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetFeedAsync_WithReversedRange_Throws()
    {
        RiskFeedService service = CreateService(new());

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetFeedAsync(
                new("PF-ALPHA", Now, Now.AddMinutes(-1)),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task IngestAsync_WithValidEvent_PersistsAndReturnsCreated()
    {
        InMemoryRiskEventRepository repository = new();
        RiskFeedService service = CreateService(repository);
        IngestRiskEventRequest request = RiskEventValidatorTests.ValidRequest();

        IngestResult result = await service.IngestAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(IngestOutcome.Created, result.Outcome);
        Assert.NotNull(result.RiskEvent);
        Assert.Equal(Now, result.RiskEvent.IngestedAtUtc);
        Assert.NotNull(
            await repository.FindAsync(
                result.RiskEvent.EventId,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task IngestAsync_WithDuplicateSequence_ReturnsDuplicate()
    {
        InMemoryRiskEventRepository repository =
            new([CreateEvent(RiskEventValidatorTests.ValidRequest().Sequence)]);
        RiskFeedService service = CreateService(repository);

        IngestResult result = await service.IngestAsync(
            RiskEventValidatorTests.ValidRequest() with { EventId = Guid.NewGuid() },
            TestContext.Current.CancellationToken);

        Assert.Equal(IngestOutcome.Duplicate, result.Outcome);
        Assert.Null(result.RiskEvent);
    }

    [Fact]
    public async Task IngestAsync_WithInvalidEvent_DoesNotPersist()
    {
        InMemoryRiskEventRepository repository = new();
        RiskFeedService service = CreateService(repository);

        IngestResult result = await service.IngestAsync(
            RiskEventValidatorTests.ValidRequest() with { Currency = "US" },
            TestContext.Current.CancellationToken);

        Assert.Equal(IngestOutcome.Rejected, result.Outcome);
        Assert.NotEmpty(result.Issues);
    }

    private RiskFeedService CreateService(InMemoryRiskEventRepository repository) =>
        new(
            repository,
            new RiskEventValidator(_timeProvider),
            _timeProvider);

    private static RiskEvent CreateEvent(long sequence) =>
        new(
            Guid.NewGuid(),
            "PF-ALPHA",
            "EURUSD",
            RiskMetric.ValueAtRisk95,
            100_000m + sequence,
            "USD",
            Now.AddSeconds(-10),
            sequence,
            "risk-engine-v1",
            Now);
}
