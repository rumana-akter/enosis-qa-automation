// ========================= tests/UI/TemplatesTests.cs =========================
// Login → Automated Campaigns (popup) → Templates → Add New Template.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using Enosis.QA.Automation.Tests.Base;
using Enosis.QA.Automation.Tests.Flows;

namespace Enosis.QA.Automation.Tests.Ui
{
    [TestFixture]
    public class TemplatesTests : PageTestBase
    {
        private const string Username = "havana4456";
        private const string Password = "Enosis123@@";

        [Test, Timeout(180_000)]
        [Category("UI"), Category("AutomatedCampaigns"), Property("Area", "Templates")]
        public async Task Open_Templates_In_Automated_Campaigns()
        {
            await AuthFlow.LoginAsync(Page, Username, Password);

            IPage acPage = await AutomatedCampaignsFlow.OpenAutomatedCampaignsAsync(Page);
            IPage templatesPage = await TemplateFlow.OpenTemplatesAsync(acPage);

            await Microsoft.Playwright.Assertions.Expect(templatesPage)
                .ToHaveURLAsync(new Regex("/template/index", RegexOptions.IgnoreCase));

            await TemplateFlow.ClickAddNewTemplateAsync(templatesPage);

            // Soft confirmation (optional)
            _ = templatesPage.Locator("#addTemplateModal, .modal.show").First.IsVisibleAsync(new() { Timeout = 1000 });
            _ = templatesPage.Locator("#TemplateName, input[name='TemplateName']").First.IsVisibleAsync(new() { Timeout = 1000 });

            // Already on Templates page and modal is open after ClickAddNewTemplateAsync(...)
            // After you’ve opened the Add Template modal:
            await TemplateFlow.CopyFromTemplateAsync(templatesPage, "Automation Email template");
            
            await TemplateFlow.SaveTemplateAsync(templatesPage, "Automation test email template");

             await TemplateFlow.DeleteTemplateByNameAsync(templatesPage, "Automation test email template");



        }
        
    }
}
