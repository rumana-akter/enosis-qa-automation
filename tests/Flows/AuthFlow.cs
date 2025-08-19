using Microsoft.Playwright;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Enosis.QA.Automation.Tests.Flows
{
    public static class AuthFlow
    {
        /// Login with element-based waits (no NetworkIdle / RunAndWaitForNavigationAsync).
        public static async Task LoginAsync(IPage page, string username, string password)
        {
            // Go to username page
            await page.GotoAsync(
                "https://dev-redirector.denticon.com/login?lo=1",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 }
            );

            // Username → CONTINUE
            await page.Locator("input[name='username']").FillAsync(username);
            await page.Locator("#btnLogin").ClickAsync();

            // Password → LOGIN
            var pwdInput = page.Locator("#txtPassword");
            await pwdInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 120_000 });
            await pwdInput.FillAsync(password);
            await page.Locator("#aLogin").ClickAsync();

            // ✅ Post-login: wait for a stable element that only exists after login
            await page.Locator("#MenuBar_aMenuUtilities")
                      .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 120_000 });

            // (Optional) also verify we left any *login* url
            await page.WaitForURLAsync(
                new Regex(@"^(?!.*(login|home/login\.aspx)).*$", RegexOptions.IgnoreCase),
                new() { Timeout = 120_000 }
            );
        }
    }
}
