using Microsoft.Playwright;
using NUnit.Framework;
using System.Threading.Tasks;
using Enosis.QA.Automation.Tests.Base;
using Enosis.QA.Automation.Tests.Flows;

namespace Enosis.QA.Automation.Tests.Ui
{
    [TestFixture]
    public class AutomatedCampaignsTests : PageTestBase
    {
        [Test]
        public async Task Open_AutomatedCampaigns_From_Menu()
        {
            // Reuse login flow (hardcoded creds as you wanted)
            await AuthFlow.LoginAsync(Page, "havana4456", "Enosis123@@");

            // Utilities -> Launch -> Automated Campaigns
            var utilities = Page.Locator("#MenuBar_aMenuUtilities");
            await utilities.HoverAsync();
            if (!await Page.GetByText("Launch", new() { Exact = true }).IsVisibleAsync())
                await utilities.ClickAsync();

            var launch = Page.GetByText("Launch", new() { Exact = true });
            await launch.WaitForAsync();
            await launch.HoverAsync();

            var acLink = Page.GetByText("Automated Campaigns", new() { Exact = true });

            // If it opens a new tab/window, capture it; otherwise it will navigate in the same tab
            IPage targetPage = Page;
            try
            {
                targetPage = await Page.RunAndWaitForPopupAsync(async () => await acLink.ClickAsync());
                await targetPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
            catch (PlaywrightException)
            {
                await acLink.ClickAsync();
                targetPage = Page;
                await targetPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }

            // TODO: add a concrete assertion once you know a unique element/URL on the AC page
            // await Microsoft.Playwright.Assertions.Expect(targetPage).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("dentigram|campaign", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        }
    }
}
