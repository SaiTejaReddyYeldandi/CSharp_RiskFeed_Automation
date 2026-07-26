using Microsoft.Playwright;

namespace RiskFeed.UiTests.Infrastructure;

public sealed class BrowserFixture : IAsyncLifetime
{
    private IPlaywright _playwright = null!;

    public IBrowser Browser { get; private set; } = null!;

    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("UI_BASE_URL")
        ?? "http://localhost:8088";

    public async ValueTask InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(
            new()
            {
                Headless = true
            });
    }

    public async ValueTask DisposeAsync()
    {
        await Browser.CloseAsync();
        _playwright.Dispose();
    }
}
