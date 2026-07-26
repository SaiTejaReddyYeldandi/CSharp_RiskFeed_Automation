using RiskFeed.Core.Domain;
using RiskFeed.Core.Models;
using RiskFeed.Core.Services;
using RiskFeed.UnitTests.TestDoubles;

namespace RiskFeed.UnitTests;

public sealed class RiskEventValidatorTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 26, 10, 30, 0, TimeSpan.Zero);
    private readonly RiskEventValidator _validator = new(new FixedTimeProvider(Now));

    [Fact]
    public void Validate_WithWellFormedEvent_ReturnsNoIssues()
    {
        IReadOnlyList<DataQualityIssue> issues = _validator.Validate(ValidRequest());

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData(null, "required")]
    [InlineData("", "required")]
    [InlineData("pf-lowercase", "invalid_format")]
    [InlineData("PF_ALPHA", "invalid_format")]
    public void Validate_WithInvalidPortfolio_ReturnsExpectedIssue(
        string? portfolioId,
        string expectedCode)
    {
        IngestRiskEventRequest request = ValidRequest() with
        {
            PortfolioId = portfolioId
        };

        IReadOnlyList<DataQualityIssue> issues = _validator.Validate(request);

        Assert.Contains(
            issues,
            issue => issue.Field == "portfolioId" && issue.Code == expectedCode);
    }

    [Fact]
    public void Validate_WithFutureTimestamp_RejectsClockSkew()
    {
        IngestRiskEventRequest request = ValidRequest() with
        {
            AsOfUtc = Now.AddMinutes(3)
        };

        IReadOnlyList<DataQualityIssue> issues = _validator.Validate(request);

        Assert.Contains(issues, issue => issue.Code == "future_timestamp");
    }

    [Fact]
    public void Validate_WithOldTimestamp_RejectsStaleData()
    {
        IngestRiskEventRequest request = ValidRequest() with
        {
            AsOfUtc = Now.AddDays(-31)
        };

        IReadOnlyList<DataQualityIssue> issues = _validator.Validate(request);

        Assert.Contains(issues, issue => issue.Code == "stale_timestamp");
    }

    [Theory]
    [InlineData(RiskMetric.ValueAtRisk95)]
    [InlineData(RiskMetric.ExpectedShortfall)]
    public void Validate_WithNegativeLossMetric_RejectsValue(RiskMetric metric)
    {
        IngestRiskEventRequest request = ValidRequest() with
        {
            Metric = metric,
            Value = -1m
        };

        IReadOnlyList<DataQualityIssue> issues = _validator.Validate(request);

        Assert.Contains(issues, issue => issue.Code == "negative_loss_measure");
    }

    [Fact]
    public void Validate_WithNegativeDelta_AllowsSignedExposure()
    {
        IngestRiskEventRequest request = ValidRequest() with
        {
            Metric = RiskMetric.DeltaExposure,
            Value = -42_000m
        };

        IReadOnlyList<DataQualityIssue> issues = _validator.Validate(request);

        Assert.Empty(issues);
    }

    [Fact]
    public void Validate_WithMultipleBadFields_ReportsEveryQualityIssue()
    {
        IngestRiskEventRequest request = ValidRequest() with
        {
            Instrument = "bad instrument",
            Currency = "usd",
            Source = "INVALID SOURCE",
            Sequence = 0,
            Value = 1_000_000_001m
        };

        IReadOnlyList<DataQualityIssue> issues = _validator.Validate(request);

        Assert.Contains(issues, issue => issue.Field == "instrument");
        Assert.Contains(issues, issue => issue.Field == "currency");
        Assert.Contains(issues, issue => issue.Field == "source");
        Assert.Contains(issues, issue => issue.Field == "sequence");
        Assert.Contains(issues, issue => issue.Code == "out_of_range");
    }

    internal static IngestRiskEventRequest ValidRequest() =>
        new(
            Guid.Parse("b87a1a3d-1f2f-4de0-9f34-14a96dac372d"),
            "PF-ALPHA",
            "EURUSD",
            RiskMetric.ValueAtRisk95,
            125_000m,
            "USD",
            Now.AddSeconds(-30),
            1011,
            "risk-engine-v1");
}
