using System.Text.Json;
using System.Text.Json.Serialization;

namespace RiskFeed.IntegrationTests.Infrastructure;

internal static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(
        JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
}
