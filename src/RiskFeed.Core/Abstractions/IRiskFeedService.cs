using RiskFeed.Core.Domain;
using RiskFeed.Core.Models;

namespace RiskFeed.Core.Abstractions;

public interface IRiskFeedService
{
    Task<RiskFeedPage> GetFeedAsync(
        RiskFeedQuery query,
        CancellationToken cancellationToken);

    Task<RiskEvent?> GetEventAsync(
        Guid eventId,
        CancellationToken cancellationToken);

    Task<IngestResult> IngestAsync(
        IngestRiskEventRequest request,
        CancellationToken cancellationToken);
}
