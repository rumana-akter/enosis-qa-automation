// tests/Flows/AutomatedCampaignsFlow.cs
using Microsoft.Playwright;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Enosis.QA.Automation.Tests.Flows
{
    public static class AutomatedCampaignsFlow
    {
        public static async Task<IPage> OpenAutomatedCampaignsAsync(IPage page)
        {
            await page.BringToFrontAsync();

            // Try via Utilities ▾ → Launch → Automated Campaigns
            try
            {
                var utilities = page.Locator("#MenuBar_aMenuUtilities");
                if (await utilities.CountAsync() > 0)
                {
                    await utilities.ClickAsync();

                    var launch = page.Locator("span.menuItemText", new() { HasTextString = "Launch" }).First;
                    if (await launch.CountAsync() > 0)
                    {
                        await launch.HoverAsync();

                        var acLink = page.Locator("a.menuItem", new() { HasTextString = "Automated Campaigns" }).First;
                        if (await acLink.CountAsync() > 0)
                        {
                            // If it opens a new window, capture it; otherwise it will navigate same tab
                            IPage target;
                            try
                            {
                                target = await page.RunAndWaitForPopupAsync(async () =>
                                {
                                    await acLink.ClickAsync();
                                });
                            }
                            catch
                            {
                                // No popup → same tab
                                await acLink.ClickAsync();
                                target = page;
                            }

                            if (target.IsClosed && target.Context.Pages.Any())
                                target = target.Context.Pages.Last();

                            await target.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                            // Ensure we end up on the AC dashboard
                            if (!Regex.IsMatch(target.Url ?? "", "/dashboard/index", RegexOptions.IgnoreCase))
                            {
                                var origin = new Uri(page.Url).GetLeftPart(UriPartial.Authority);
                                await target.GotoAsync($"{origin}/Dashboard/Index",
                                    new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30000 });
                            }
                            return target;
                        }
                    }
                }
            }
            catch { /* fall back below */ }

            // Fallback: direct nav
            var baseUrl = new Uri(page.Url).GetLeftPart(UriPartial.Authority);
            await page.GotoAsync($"{baseUrl}/Dashboard/Index",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30000 });
            return page;
        }
    }
}
