using Microsoft.Playwright;
using RiskFeed.UiTests.Infrastructure;

namespace RiskFeed.UiTests;

[Collection(BrowserCollection.Name)]
public sealed class DashboardTests(BrowserFixture fixture)
{
    [Fact]
    public async Task Dashboard_LoadsRiskChartAndPassingQualityControls()
    {
        await using IBrowserContext context =
            await fixture.Browser.NewContextAsync();
        IPage page = await context.NewPageAsync();

        await page.GotoAsync(
            BrowserFixture.BaseUrl,
            new()
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new()
        {
            Name = "Risk feed control room"
        })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("risk-chart")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("quality-status"))
            .ToHaveTextAsync("PASS");
        await Assertions.Expect(page.GetByTestId("validated-records"))
            .ToHaveTextAsync("10");
        await Assertions.Expect(
                page.GetByTestId("risk-events-table").Locator("tbody tr"))
            .ToHaveCountAsync(8);
        await CaptureEvidenceAsync(page, "dashboard-desktop.png");
    }

    [Fact]
    public async Task PortfolioSelection_RefreshesChartFromRiskFeedApi()
    {
        await using IBrowserContext context =
            await fixture.Browser.NewContextAsync();
        IPage page = await context.NewPageAsync();
        await page.GotoAsync(
            BrowserFixture.BaseUrl,
            new()
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

        Task<IResponse> responseTask = page.WaitForResponseAsync(
            response =>
                response.Url.Contains("portfolioId=PF-BETA", StringComparison.Ordinal)
                && response.Status == 200);
        await page.GetByTestId("portfolio-select").SelectOptionAsync("PF-BETA");
        await responseTask;

        await Assertions.Expect(page.GetByTestId("validated-records"))
            .ToHaveTextAsync("8");
        await Assertions.Expect(
                page.GetByTestId("risk-events-table").Locator("tbody tr").First)
            .ToContainTextAsync("XAUUSD");
        await Assertions.Expect(page.GetByTestId("quality-status"))
            .ToHaveTextAsync("PASS");
    }

    [Fact]
    public async Task MobileViewport_RendersControlsWithoutHorizontalOverflow()
    {
        await using IBrowserContext context =
            await fixture.Browser.NewContextAsync(
                new()
                {
                    ViewportSize = new()
                    {
                        Width = 390,
                        Height = 844
                    },
                    IsMobile = true
                });
        IPage page = await context.NewPageAsync();
        await page.GotoAsync(
            BrowserFixture.BaseUrl,
            new()
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

        await Assertions.Expect(page.GetByTestId("portfolio-select"))
            .ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("risk-chart")).ToBeVisibleAsync();
        int scrollWidth = await page.EvaluateAsync<int>(
            "() => document.documentElement.scrollWidth");
        int clientWidth = await page.EvaluateAsync<int>(
            "() => document.documentElement.clientWidth");

        Assert.True(
            scrollWidth <= clientWidth,
            $"The page overflows horizontally: {scrollWidth}px > {clientWidth}px.");
        await CaptureEvidenceAsync(page, "dashboard-mobile.png");
    }

    private static async Task CaptureEvidenceAsync(IPage page, string fileName)
    {
        string? artifactDirectory =
            Environment.GetEnvironmentVariable("UI_ARTIFACT_DIR");

        if (string.IsNullOrWhiteSpace(artifactDirectory))
        {
            return;
        }

        Directory.CreateDirectory(artifactDirectory);
        await page.ScreenshotAsync(
            new()
            {
                Path = Path.Combine(artifactDirectory, fileName),
                FullPage = true
            });
    }
}
