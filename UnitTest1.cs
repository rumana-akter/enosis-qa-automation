using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Enosis.QA.Automation
{
    [TestFixture]
    public class SmokeTests : PageTest
    {
        [Test]
        public async Task Can_Open_Playwright_Docs()
        {
            await Page.GotoAsync("https://playwright.dev/dotnet");
            await Expect(Page).ToHaveTitleAsync(new Regex("Playwright"));
        }
    }
}
