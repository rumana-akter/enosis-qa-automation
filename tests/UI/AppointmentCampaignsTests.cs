using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.Playwright;
using Enosis.QA.Automation.Tests.Base;   // PageTestBase
using Enosis.QA.Automation.Tests.Flows;  // AuthFlow, AutomatedCampaignsFlow, AppointmentCampaignsFlow

namespace Enosis.QA.Automation.Tests.Ui
{
    [TestFixture]
    public class AppointmentCampaignsTests : PageTestBase
    {
        private const string Username = "havana4456";
        private const string Password = "Enosis123@@";

        [Test, Timeout(240_000)]
        public async Task Create_Appointment_Campaign_Fill_Name_Select_MailingList_Template_And_Save()
        {
            // 1) Login on legacy shell
            await AuthFlow.LoginAsync(Page, Username, Password);

            // 2) Utilities -> Automated Campaigns (child window/page)
            IPage acPage = await AutomatedCampaignsFlow.OpenAutomatedCampaignsAsync(Page);

            // 3) Open Appointment tab (grid)
            await AppointmentCampaignsFlow.OpenAppointmentTabAsync(acPage);

            // 4) Go to creation page ONCE
            await AppointmentCampaignsFlow.NavigateToRecurringCreateAsync(acPage);

            // 5) Fill Campaign Name
            var campaignName = $"Automation App Campaign {DateTime.UtcNow:yyyyMMdd_HHmmss}";
            await AppointmentCampaignsFlow.FillCampaignNameAsync(acPage, campaignName);

            // 6) Mailing List -> pick R.A_101
            await AppointmentCampaignsFlow.SelectMailingListAsync(
                acPage,
                searchTerm: "R.A_101",
                expectedLabel: "R.A_101",
                currentLabel: "NA all"
            );

            // 7) Touchpoint: set Text Message
            await AppointmentCampaignsFlow.SelectTextMessageDeliveryAsync(acPage, rowIndex: 0);

            // 8) Template -> select "Automation SMS template"
            await AppointmentCampaignsFlow.SelectTemplateAsync(
                acPage,
                searchTerm: "Automation SMS template",
                expectedLabel: "Automation SMS template",
                rowIndex: 0
            );

            // 9) SAVE on the creation form -> return to grid (Dashboard/Index) and verify row
            //await AppointmentCampaignsFlow.SaveOnCreateAndReturnToGridAsync(acPage, campaignName);

            await AppointmentCampaignsFlow.SaveOnCreateAndStayOnDashboardAsync(acPage, campaignName);

            // 10) Guard: ensure we did NOT navigate back to the create page
            var url = acPage.Url;
            StringAssert.Contains("/Dashboard/Index", url, "We should remain on the grid after saving.");
            StringAssert.DoesNotContain("/campaign/AppointmentRecurring", url, "We must not bounce back to the create page.");
        }
    }
}
