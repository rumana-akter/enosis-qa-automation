using Microsoft.Playwright;
using NUnit.Framework;

namespace Enosis.QA.Automation.Tests.Base
{
    public class PageTestBase
    {
        protected IPlaywright _pw = default!;
        protected IBrowser _browser = default!;
        protected IBrowserContext _ctx = default!;
        protected IPage Page = default!;

        [SetUp]
        public async Task SetUp()
        {
            _pw = await Playwright.CreateAsync();
            _browser = await _pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,   // 👈 show real browser
                SlowMo = 300     // 👈 slow steps so you can see them
            });

            _ctx = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true,
                RecordVideoDir = "videos",
                ViewportSize = new() { Width = 1280, Height = 800 }
            });

            _ctx.SetDefaultTimeout(60_000);             // 60s for locator waits
            _ctx.SetDefaultNavigationTimeout(120_000);  // 120s for navigations

            await _ctx.Tracing.StartAsync(new TracingStartOptions
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });

            Page = await _ctx.NewPageAsync();
        }

        [TearDown]
        public async Task TearDown()
        {
            var name = TestContext.CurrentContext.Test.Name.Replace(" ", "_");
            await _ctx.Tracing.StopAsync(new TracingStopOptions { Path = $"trace/{name}.zip" });
            await _ctx.CloseAsync();
            await _browser.CloseAsync();
            _pw.Dispose();
        }
    }
}
