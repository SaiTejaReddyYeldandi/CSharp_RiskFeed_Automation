using RiskFeed.Core.Abstractions;
using RiskFeed.Core.Domain;
using RiskFeed.Core.Models;

namespace RiskFeed.Core.Services;

public sealed class RiskFeedService(
    IRiskEventRepository repository,
    IRiskEventValidator validator,
    TimeProvider timeProvider) : IRiskFeedService
{
    public async Task<RiskFeedPage> GetFeedAsync(
        RiskFeedQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query.PortfolioId);

        if (query.Limit is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Feed limit must be between 1 and 500.");
        }

        if (query.FromUtc > query.ToUtc)
        {
            throw new ArgumentException(
                "From timestamp must be earlier than or equal to the to timestamp.",
                nameof(query));
        }

        IReadOnlyList<RiskEvent> events =
            await repository.QueryAsync(query, cancellationToken);
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();

        return new(
            events,
            DataQualitySummary.Evaluate(events, nowUtc),
            nowUtc);
    }

    public Task<RiskEvent?> GetEventAsync(
        Guid eventId,
        CancellationToken cancellationToken) =>
        repository.FindAsync(eventId, cancellationToken);

    public async Task<IngestResult> IngestAsync(
        IngestRiskEventRequest request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<DataQualityIssue> issues = validator.Validate(request);

        if (issues.Count > 0)
        {
            return new(IngestOutcome.Rejected, null, issues);
        }

        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        RiskEvent riskEvent = new(
            request.EventId ?? Guid.NewGuid(),
            request.PortfolioId!,
            request.Instrument!,
            request.Metric,
            request.Value,
            request.Currency!,
            request.AsOfUtc,
            request.Sequence,
            request.Source!,
            nowUtc);

        bool inserted = await repository.InsertAsync(riskEvent, cancellationToken);

        return inserted
            ? new(IngestOutcome.Created, riskEvent, [])
            : new(IngestOutcome.Duplicate, null, []);
    }
}
