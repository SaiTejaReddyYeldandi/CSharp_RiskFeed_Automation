using RiskFeed.Core.Models;

namespace RiskFeed.Core.Abstractions;

public interface IRiskEventValidator
{
    IReadOnlyList<DataQualityIssue> Validate(IngestRiskEventRequest request);
}
