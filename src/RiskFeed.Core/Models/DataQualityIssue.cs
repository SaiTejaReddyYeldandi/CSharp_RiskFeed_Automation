namespace RiskFeed.Core.Models;

public sealed record DataQualityIssue(
    string Field,
    string Code,
    string Message);
