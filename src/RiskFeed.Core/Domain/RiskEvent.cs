namespace RiskFeed.Core.Domain;

public sealed record RiskEvent(
    Guid EventId,
    string PortfolioId,
    string Instrument,
    RiskMetric Metric,
    decimal Value,
    string Currency,
    DateTimeOffset AsOfUtc,
    long Sequence,
    string Source,
    DateTimeOffset IngestedAtUtc);
