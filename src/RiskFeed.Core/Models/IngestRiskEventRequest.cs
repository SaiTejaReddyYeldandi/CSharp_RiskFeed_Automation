using RiskFeed.Core.Domain;

namespace RiskFeed.Core.Models;

public sealed record IngestRiskEventRequest(
    Guid? EventId,
    string? PortfolioId,
    string? Instrument,
    RiskMetric Metric,
    decimal Value,
    string? Currency,
    DateTimeOffset AsOfUtc,
    long Sequence,
    string? Source);
