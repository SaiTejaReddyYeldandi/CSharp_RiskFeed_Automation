namespace RiskFeed.Api;

public static class ProblemTypes
{
    private const string BaseUri = "https://risk-feed.example/problems";

    public const string DataQuality = $"{BaseUri}/data-quality";
    public const string Duplicate = $"{BaseUri}/duplicate";
    public const string Forbidden = $"{BaseUri}/forbidden";
    public const string InvalidQuery = $"{BaseUri}/invalid-query";
    public const string NotFound = $"{BaseUri}/not-found";
    public const string Unauthorized = $"{BaseUri}/unauthorized";
}
