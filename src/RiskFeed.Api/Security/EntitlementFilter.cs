namespace RiskFeed.Api.Security;

public static class Entitlements
{
    public const string RiskRead = "risk:read";
    public const string RiskWrite = "risk:write";
}

public sealed class EntitlementFilter(string requiredEntitlement) : IEndpointFilter
{
    private const string ClientIdHeader = "X-Client-Id";
    private const string EntitlementsHeader = "X-Entitlements";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        IHeaderDictionary headers = context.HttpContext.Request.Headers;

        if (!headers.TryGetValue(ClientIdHeader, out var clientId)
            || string.IsNullOrWhiteSpace(clientId))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Client identity is required.",
                detail: $"Provide the {ClientIdHeader} header.",
                type: ProblemTypes.Unauthorized);
        }

        string[] entitlements = headers[EntitlementsHeader]
            .ToString()
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries);

        if (!entitlements.Contains(
                requiredEntitlement,
                StringComparer.OrdinalIgnoreCase))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Required entitlement is missing.",
                detail: $"The client requires '{requiredEntitlement}'.",
                type: ProblemTypes.Forbidden);
        }

        return await next(context);
    }
}
