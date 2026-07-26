using RiskFeed.Core.Abstractions;
using RiskFeed.Core.Domain;
using RiskFeed.Core.Models;

namespace RiskFeed.UnitTests.TestDoubles;

internal sealed class InMemoryRiskEventRepository(
    IEnumerable<RiskEvent>? seed = null) : IRiskEventRepository
{
    private readonly List<RiskEvent> _events = seed?.ToList() ?? [];

    public Task<IReadOnlyList<RiskEvent>> QueryAsync(
        RiskFeedQuery query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<RiskEvent> result = _events
            .Where(item => item.PortfolioId == query.PortfolioId)
            .Where(item => !query.FromUtc.HasValue || item.AsOfUtc >= query.FromUtc)
            .Where(item => !query.ToUtc.HasValue || item.AsOfUtc <= query.ToUtc)
            .OrderByDescending(item => item.Sequence)
            .Take(query.Limit)
            .ToArray();
        return Task.FromResult(result);
    }

    public Task<RiskEvent?> FindAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _events.SingleOrDefault(item => item.EventId == eventId));
    }

    public Task<bool> InsertAsync(
        RiskEvent riskEvent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool duplicate = _events.Any(
            item => item.EventId == riskEvent.EventId
                || item.PortfolioId == riskEvent.PortfolioId
                && item.Sequence == riskEvent.Sequence);

        if (!duplicate)
        {
            _events.Add(riskEvent);
        }

        return Task.FromResult(!duplicate);
    }
}
