namespace RiskFeed.Core.Models;

public sealed record RiskFeedQuery(
    string PortfolioId,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    int Limit = 100);
