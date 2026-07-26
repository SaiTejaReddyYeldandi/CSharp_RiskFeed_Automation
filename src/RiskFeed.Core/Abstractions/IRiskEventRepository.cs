using RiskFeed.Core.Domain;
using RiskFeed.Core.Models;

namespace RiskFeed.Core.Abstractions;

public interface IRiskEventRepository
{
    Task<IReadOnlyList<RiskEvent>> QueryAsync(
        RiskFeedQuery query,
        CancellationToken cancellationToken);

    Task<RiskEvent?> FindAsync(
        Guid eventId,
        CancellationToken cancellationToken);

    Task<bool> InsertAsync(
        RiskEvent riskEvent,
        CancellationToken cancellationToken);
}
