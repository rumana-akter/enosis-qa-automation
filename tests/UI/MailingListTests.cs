// tests/UI/MailingListTests.cs
using Microsoft.Playwright;
using NUnit.Framework;
using System.Threading.Tasks;
using Enosis.QA.Automation.Tests.Base;
using Enosis.QA.Automation.Tests.Flows;

namespace Enosis.QA.Automation.Tests.Ui
{
    [TestFixture]  //-----------------------------------------run full ML -----------------------------------
    public class MailingListTests : PageTestBase
    {
        // --- test data (hard-coded for now) ---
        private const string Username = "havana4456";
        private const string Password = "Enosis123@@";
        private const string ListName = "automation1";
        private const string OfficeSearch = "101";
        private const string OfficeId = "101";
        private const string OfficeLabel = "Organic Dentistry (101)";

        [Test, Timeout(180_000)]
        public async Task Create_Then_Delete_MailingList()
        {
            // 1) Login
            await AuthFlow.LoginAsync(Page, Username, Password);

            // 2) Open Automated Campaigns (returns the child window/page)
            IPage acPage = await AutomatedCampaignsFlow.OpenAutomatedCampaignsAsync(Page);

            // 3) Open Mailing List and click "+ ADD CUSTOM MAILING LIST"
            await MailingListFlow.OpenAndClickAddAsync(acPage);

            // 4) Fill the list name and open the Office/Group selector
            await MailingListFlow.FillNameAndOpenOfficeSelectAsync(acPage, ListName);

            // 5) In the selector modal: search "101" -> pick "Organic Dentistry (101)" -> move right -> Apply
            await MailingListFlow.SelectOfficeBySearchAndApplyAsync(acPage, OfficeSearch, OfficeId, OfficeLabel);

            // 6) Save & Confirm (wait briefly for the new list to appear/close modal)
            await MailingListFlow.SaveAndConfirmAsync(acPage, ListName, appearTimeoutMs: 2500);

            // 7) Delete the list we just created (handles the Bootbox confirm dialog)
            await MailingListFlow.DeleteMailingListAsync(acPage, ListName);
        }
    }
}
