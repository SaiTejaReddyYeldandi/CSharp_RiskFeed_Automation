using RiskFeed.Core.Domain;

namespace RiskFeed.Core.Models;

public sealed record RiskFeedPage(
    IReadOnlyList<RiskEvent> Items,
    DataQualitySummary DataQuality,
    DateTimeOffset GeneratedAtUtc);
