using System.Net.Http.Headers;
using NBomber.Contracts;
using NBomber.CSharp;

string baseUrl = ReadString("PERF_BASE_URL", "http://localhost:8088");
int ratePerSecond = ReadInt("PERF_RATE", 25);
int durationSeconds = ReadInt("PERF_DURATION_SECONDS", 30);
double maximumErrorPercent = ReadDouble("PERF_MAX_ERROR_PERCENT", 1);
double maximumP95Milliseconds = ReadDouble("PERF_MAX_P95_MS", 300);
string reportFolder = Path.GetFullPath(
    Environment.GetEnvironmentVariable("PERF_REPORT_FOLDER")
    ?? Path.Combine(".artifacts", "performance"));

using HttpClient client = new()
{
    BaseAddress = new Uri(baseUrl),
    Timeout = TimeSpan.FromSeconds(5)
};
client.DefaultRequestHeaders.Add("X-Client-Id", "nbomber-load-test");
client.DefaultRequestHeaders.Add("X-Entitlements", "risk:read");
client.DefaultRequestHeaders.Accept.Add(
    new MediaTypeWithQualityHeaderValue("application/json"));

var scenario = Scenario
    .Create("risk_feed_read", async _ =>
    {
        try
        {
            using HttpResponseMessage response = await client.GetAsync(
                "/api/v1/risk-feed?portfolioId=PF-ALPHA&limit=10");
            string statusCode = ((int)response.StatusCode).ToString(
                System.Globalization.CultureInfo.InvariantCulture);

            return response.IsSuccessStatusCode
                ? Response.Ok(statusCode: statusCode)
                : Response.Fail(statusCode: statusCode);
        }
        catch (Exception exception)
        {
            return Response.Fail(message: exception.Message);
        }
    })
    .WithoutWarmUp()
    .WithLoadSimulations(
        Simulation.Inject(
            rate: ratePerSecond,
            interval: TimeSpan.FromSeconds(1),
            during: TimeSpan.FromSeconds(durationSeconds)))
    .WithThresholds(
        Threshold.Create(
            stats => stats.Fail.Request.Percent < maximumErrorPercent),
        Threshold.Create(
            stats => stats.Ok.Latency.Percent95 < maximumP95Milliseconds));

Directory.CreateDirectory(reportFolder);
var result = NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportFolder(reportFolder)
    .Run();

var failedThresholds = result.Thresholds
    .Where(threshold => threshold.IsFailed)
    .ToArray();

if (failedThresholds.Length == 0)
{
    Console.WriteLine(
        $"PERFORMANCE GATE PASSED: errors < {maximumErrorPercent}% and p95 < {maximumP95Milliseconds} ms.");
    return 0;
}

Console.Error.WriteLine("PERFORMANCE GATE FAILED:");
foreach (var threshold in failedThresholds)
{
    Console.Error.WriteLine($"- {threshold}");
}

return 2;

static string ReadString(string name, string fallback) =>
    Environment.GetEnvironmentVariable(name) ?? fallback;

static int ReadInt(string name, int fallback) =>
    int.TryParse(
        Environment.GetEnvironmentVariable(name),
        System.Globalization.NumberStyles.Integer,
        System.Globalization.CultureInfo.InvariantCulture,
        out int value)
        ? value
        : fallback;

static double ReadDouble(string name, double fallback) =>
    double.TryParse(
        Environment.GetEnvironmentVariable(name),
        System.Globalization.NumberStyles.Float,
        System.Globalization.CultureInfo.InvariantCulture,
        out double value)
        ? value
        : fallback;
