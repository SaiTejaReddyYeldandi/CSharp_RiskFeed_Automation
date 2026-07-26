using RiskFeed.Core.Domain;

namespace RiskFeed.Core.Models;

public enum IngestOutcome
{
    Created,
    Rejected,
    Duplicate
}

public sealed record IngestResult(
    IngestOutcome Outcome,
    RiskEvent? RiskEvent,
    IReadOnlyList<DataQualityIssue> Issues);
