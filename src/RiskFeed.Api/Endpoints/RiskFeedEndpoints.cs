using Microsoft.AspNetCore.Mvc;
using RiskFeed.Api.Security;
using RiskFeed.Core.Abstractions;
using RiskFeed.Core.Domain;
using RiskFeed.Core.Models;

namespace RiskFeed.Api.Endpoints;

public static class RiskFeedEndpoints
{
    public static IEndpointRouteBuilder MapRiskFeedEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/api/v1/risk-feed")
            .WithTags("Risk Feed");

        group.MapGet("/", GetFeedAsync)
            .WithName("GetRiskFeed")
            .WithSummary("Returns validated risk events for an entitled portfolio.")
            .AddEndpointFilter(new EntitlementFilter(Entitlements.RiskRead))
            .Produces<RiskFeedPage>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{eventId:guid}", GetEventAsync)
            .WithName("GetRiskEvent")
            .WithSummary("Returns a single risk event by event identifier.")
            .AddEndpointFilter(new EntitlementFilter(Entitlements.RiskRead))
            .Produces<RiskEvent>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", IngestAsync)
            .WithName("IngestRiskEvent")
            .WithSummary("Validates and ingests a risk event.")
            .AddEndpointFilter(new EntitlementFilter(Entitlements.RiskWrite))
            .Produces<RiskEvent>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> GetFeedAsync(
        string portfolioId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int? limit,
        IRiskFeedService service,
        CancellationToken cancellationToken)
    {
        try
        {
            RiskFeedPage page = await service.GetFeedAsync(
                new(
                    portfolioId.Trim().ToUpperInvariant(),
                    fromUtc,
                    toUtc,
                    limit ?? 100),
                cancellationToken);
            return Results.Ok(page);
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid risk feed query.",
                detail: exception.Message,
                type: ProblemTypes.InvalidQuery);
        }
    }

    private static async Task<IResult> GetEventAsync(
        Guid eventId,
        IRiskFeedService service,
        CancellationToken cancellationToken)
    {
        RiskEvent? riskEvent =
            await service.GetEventAsync(eventId, cancellationToken);

        return riskEvent is null
            ? Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Risk event not found.",
                detail: $"No risk event exists with id '{eventId}'.",
                type: ProblemTypes.NotFound)
            : Results.Ok(riskEvent);
    }

    private static async Task<IResult> IngestAsync(
        IngestRiskEventRequest request,
        IRiskFeedService service,
        CancellationToken cancellationToken)
    {
        IngestResult result =
            await service.IngestAsync(request, cancellationToken);

        if (result.Outcome == IngestOutcome.Rejected)
        {
            Dictionary<string, string[]> errors = result.Issues
                .GroupBy(issue => issue.Field, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(issue => $"{issue.Code}: {issue.Message}")
                        .ToArray(),
                    StringComparer.Ordinal);

            return Results.ValidationProblem(
                errors,
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Risk feed data quality validation failed.",
                type: ProblemTypes.DataQuality);
        }

        if (result.Outcome == IngestOutcome.Duplicate)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate risk event.",
                detail: "The event id or portfolio sequence already exists.",
                type: ProblemTypes.Duplicate);
        }

        RiskEvent created = result.RiskEvent!;
        return Results.Created(
            $"/api/v1/risk-feed/{created.EventId}",
            created);
    }
}
