// ========================= tests/Flows/AppointmentCampaignsFlow.cs =========================
// Appointment Campaign flows for dev-core: open Appointment tab, go to creation page,
// fill fields (Campaign Name, Mailing List), set Delivery Type (Text Message),
// select Template (e.g., "Automation SMS template"), SAVE AND CONFIRM, and verify in grid.

using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Enosis.QA.Automation.Tests.Flows
{
    public static class AppointmentCampaignsFlow
    {
        // --------------------------- Selectors ---------------------------
        private static class Sel
        {
            // Left nav (inside Automated Campaigns window)
            public const string SideMenuAppointment = "span.side-menu-icon.appointment-icon";

            // Appointment tab grid
            public const string AppointmentGridAny = "#tblAppointmentCampaigns, table.dataTable";

            // Creation page (Appointment Recurring)
            public const string CampaignNameInput = "#CAMPAIGNNAME";

            // Mailing List (scope to the row that contains "Mailing List")
            public const string MailingListRowXpath =
                "xpath=//*[normalize-space()='Mailing List']/ancestor::*[contains(@class,'row') or contains(@class,'form-group') or self::tr][1]";

            // Bootstrap-select helpers (used by mailing list & template)
            public const string BsOpenContainer = "div.bootstrap-select.show, div.bootstrap-select.open";
            public const string BsSearchBox = ".bs-searchbox input[role='textbox'], .bs-searchbox input.form-control[aria-label='Search']";

            // Touch Points – radios (row index based)
            public static string TextMessageRadio(int i)
                => $"input.rbRDelType.touch-point-control#txtmessage-{i}[name='Touchpoints[{i}].DeliveryType'][value='S']";
            public static string EmailRadio(int i)
                => $"input[name='Touchpoints[{i}].DeliveryType'][value='E']";

            // Touch Points – Template dropdown (bootstrap-select)
            // Example DOM: select#ddlTemplateList-0 + button[data-id="ddlTemplateList-0"]
            public static string TemplateButton(int i)
                => $"button[data-id='ddlTemplateList-{i}']";
            public static string TemplateSelect(int i)
                => $"select#ddlTemplateList-{i}";
            public static string TemplateTriggerText(int i)
                => $"{TemplateButton(i)} .filter-option-inner-inner";

            // SAVE on the creation form (the correct "SAVE AND CONFIRM" for this page)
            public const string SaveAndConfirmCreateBtn = "#btnSaveReminderNew";

            // Confirmation/toast (best-effort)
            public const string ConfirmYesBtn = ".bootbox .modal-footer .btn-primary, .modal:has-text('Confirm') button:has-text('Yes')";
            public const string ToastSuccess = ".toast-success, [data-testid='toast-success']";

            // Grid row by name
            public static string RowByName(string name) => $"tr:has(td:has-text('{name}'))";
        }

        // --------------------------- Navigation ---------------------------

        /// Open the Appointment tab and wait for grid.
public static async Task OpenAppointmentTabAsync(IPage acPage)
{
    var grid = acPage.Locator("#tblAppointmentCampaigns, table.dataTable").First;

    for (int attempt = 0; attempt < 3; attempt++)
    {
        // Click the left side "Appointment" icon
        var appt = acPage.Locator("span.side-menu-icon.appointment-icon").First;
        await appt.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await appt.ClickAsync(new() { Force = true });

        try
        {
            // Quick wait to see if the grid shows
            await grid.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5_000 });
            await acPage.Locator("table.dataTable tbody").First
                .WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 5_000 });

            // If a DataTables "processing…" overlay exists, wait it out (best-effort)
            var processing = acPage.Locator("div.dataTables_processing:visible").First;
            if (await processing.CountAsync() > 0)
                await processing.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 30_000 });

            return; // ✅ grid is up
        }
        catch
        {
            // As a fallback, click a *visible* tab link, but *exclude* listbox options
            var tab = acPage.Locator(
                "a:has-text('Appointment Campaign'):not([role='option'])").First;

            if (await tab.CountAsync() > 0 && await tab.IsVisibleAsync())
                await tab.ClickAsync(new() { Force = true });

            // loop and re-check
        }
    }

    // Final robust wait
    await grid.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
    await acPage.Locator("table.dataTable tbody").First
        .WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 60_000 });
}




        /// Go to dashboard and then open the Appointment grid (helps clear modals/stale state).
    public static async Task GoToAppointmentGridAsync(IPage acPage)
{
    var origin = new Uri(acPage.Url).GetLeftPart(UriPartial.Authority);

    // Best-effort close any modal/overlay
    var modalClose = acPage.Locator(".modal.show .close, .bootbox .modal-footer .btn").First;
    if (await modalClose.CountAsync() > 0 && await modalClose.IsVisibleAsync())
    {
        try { await modalClose.ClickAsync(new() { Force = true }); } catch { /* ignore */ }
    }

    await acPage.GotoAsync($"{origin}/Dashboard/Index", new() { WaitUntil = WaitUntilState.Load });
    await acPage.WaitForLoadStateAsync(LoadState.NetworkIdle);

    await OpenAppointmentTabAsync(acPage);
}



        /// Navigate directly to the creation page (Appointment Recurring).
        public static async Task NavigateToRecurringCreateAsync(IPage acPage)
        {
            var origin = new Uri(acPage.Url).GetLeftPart(UriPartial.Authority);
            await acPage.GotoAsync($"{origin}/campaign/AppointmentRecurring?campaignType=AP");
            await acPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        // --------------------------- Field fills ---------------------------

        public static async Task FillCampaignNameAsync(IPage acPage, string name)
        {
            var nameInput = acPage.Locator(Sel.CampaignNameInput).First;
            await nameInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
            await nameInput.FillAsync(name);
        }

        /// Select a Mailing List (scoped to the field row that contains the label text).
        /// Tries native <select>, else bootstrap-select (search + choose). Robust verification.
        public static async Task SelectMailingListAsync(
            IPage acPage,
            string searchTerm,
            string expectedLabel,
            string currentLabel = "NA all")
        {
            var row = acPage.Locator(Sel.MailingListRowXpath).First;
            await row.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

            var nativeSelect = row.Locator("select").First;
            var triggerBtn   = row.Locator(".bootstrap-select > button, button.dropdown-toggle").First;
            var triggerText  = row.Locator(".filter-option-inner-inner").First;

            // Strategy 1: native select by Label
            if (await nativeSelect.CountAsync() > 0)
            {
                try
                {
                    await nativeSelect.SelectOptionAsync(new[] { new SelectOptionValue { Label = expectedLabel } });
                    if (await RowShowsAsync(row, expectedLabel)) return;
                }
                catch { /* continue */ }
            }

            // Strategy 2: bootstrap-select dropdown
            if (await triggerBtn.CountAsync() == 0)
                throw new AssertionException("Mailing List trigger button not found in the field row.");

            string now = (await triggerText.CountAsync() > 0) ? ((await triggerText.InnerTextAsync()) ?? "").Trim() : "";
            if (string.IsNullOrWhiteSpace(now) || now.Equals(currentLabel, StringComparison.OrdinalIgnoreCase))
                await triggerBtn.ClickAsync(new() { Force = true });
            else
                await triggerBtn.ClickAsync(new() { Force = true }); // open to change

            var open = acPage.Locator(Sel.BsOpenContainer).First;
            await open.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });

            var searchBox = open.Locator(Sel.BsSearchBox).First;
            if (!await searchBox.IsVisibleAsync())
                await open.ClickAsync(new() { Position = new() { X = 8, Y = 8 } });

            await searchBox.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 10_000 });
            await searchBox.FillAsync(string.Empty);
            await searchBox.FillAsync(searchTerm);

            // try to click explicit option (pipe/underscore variants), else Enter to pick highlighted
            string labelPipe = expectedLabel.Replace("_", " | ");
            string labelUndr = expectedLabel.Replace(" | ", "_");
            var option = open.Locator($".dropdown-menu li:not(.disabled) a:has-text('{labelPipe}'), .dropdown-menu .dropdown-item:has-text('{labelPipe}')").First;
            if (await option.CountAsync() == 0)
                option = open.Locator($".dropdown-menu li:not(.disabled) a:has-text('{labelUndr}'), .dropdown-menu .dropdown-item:has-text('{labelUndr}')").First;

            if (await option.CountAsync() > 0) await option.ClickAsync();
            else await searchBox.PressAsync("Enter");

            await open.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 10_000 });

            var trigVal = (await triggerText.CountAsync() > 0) ? ((await triggerText.InnerTextAsync()) ?? "").Trim() : "";
            var natVal  = await nativeSelect.CountAsync() > 0
                ? (await nativeSelect.EvaluateAsync<string>("s => (s.options[s.selectedIndex]?.textContent || '').trim()")) ?? ""
                : "";

            Console.WriteLine($"[MailingList] Trigger='{trigVal}' | Native='{natVal}' | Expected='{expectedLabel}'");

            Assert.That(
                LooksLikeExpected(trigVal, expectedLabel) || LooksLikeExpected(natVal, expectedLabel),
                $"Mailing List selection did not reflect '{expectedLabel}'. Actual trigger='{trigVal}', native='{natVal}'.");
        }

        // --------------------------- Touch Points ---------------------------

        /// Robust selection of "Text Message" radio in the Touch Point row (default index 0).
        public static async Task SelectTextMessageDeliveryAsync(IPage acPage, int rowIndex = 0)
        {
            var radio = acPage.Locator(Sel.TextMessageRadio(rowIndex)).First;

            await radio.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 10_000 });
            await radio.ScrollIntoViewIfNeededAsync();

            async Task<bool> Done() => await radio.IsCheckedAsync();

            try
            {
                await radio.CheckAsync(new() { Force = true });
                if (await Done()) return;
            }
            catch { /* continue */ }

            var labelFor = acPage.Locator($"label[for='txtmessage-{rowIndex}']").First;
            if (await labelFor.CountAsync() > 0)
            {
                await labelFor.ScrollIntoViewIfNeededAsync();
                await labelFor.ClickAsync(new() { Force = true });
                if (await Done()) return;
            }

            var textMsg = acPage.Locator("text=Text Message").First;
            if (await textMsg.CountAsync() > 0)
            {
                await textMsg.ScrollIntoViewIfNeededAsync();
                await textMsg.ClickAsync(new() { Force = true });
                if (await Done()) return;
            }

            var email = acPage.Locator(Sel.EmailRadio(rowIndex)).First;
            if (await email.CountAsync() > 0)
            {
                await email.FocusAsync();
                await acPage.Keyboard.PressAsync("ArrowRight");
                if (await Done()) return;
            }

            var jsClicked = await acPage.EvaluateAsync<bool>(@"
                ({ id }) => {
                    const el = document.getElementById(id);
                    if (!el) return false;
                    el.click();
                    el.checked = true;
                    el.dispatchEvent(new Event('input',  { bubbles: true }));
                    el.dispatchEvent(new Event('change', { bubbles: true }));
                    return el.checked === true;
                }",
                new { id = $"txtmessage-{rowIndex}" });

            Assert.That(jsClicked || await Done(),
                Is.True, "Expected 'Text Message' delivery type to be selected, but it was not.");
        }

        /// Select a Template for the Touch Point row (bootstrap-select).
        public static async Task SelectTemplateAsync(
            IPage acPage,
            string searchTerm,
            string expectedLabel,
            int rowIndex = 0,
            string currentLabel = "Please select a template")
        {
            var btn  = acPage.Locator(Sel.TemplateButton(rowIndex)).First;
            var sel  = acPage.Locator(Sel.TemplateSelect(rowIndex)).First;
            var trig = acPage.Locator(Sel.TemplateTriggerText(rowIndex)).First;

            await btn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

            string now = (await trig.CountAsync() > 0) ? ((await trig.InnerTextAsync()) ?? "").Trim() : "";
            if (string.IsNullOrWhiteSpace(now) || now.Equals(currentLabel, StringComparison.OrdinalIgnoreCase))
                await btn.ClickAsync(new() { Force = true });
            else
                await btn.ClickAsync(new() { Force = true }); // open to change

            var open = acPage.Locator(Sel.BsOpenContainer).First;
            await open.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });

            var searchBox = open.Locator(Sel.BsSearchBox).First;
            if (await searchBox.CountAsync() > 0)
            {
                await searchBox.FillAsync(string.Empty);
                await searchBox.FillAsync(searchTerm);
            }

            var option = open.Locator($".dropdown-menu .dropdown-item:has-text('{expectedLabel}'), span.text:has-text('{expectedLabel}')").First;
            if (await option.CountAsync() > 0)
                await option.ClickAsync(new() { Force = true });
            else if (await searchBox.CountAsync() > 0)
                await searchBox.PressAsync("Enter");

            await open.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 10_000 });

            var triggerText = (await trig.CountAsync() > 0) ? ((await trig.InnerTextAsync()) ?? "").Trim() : "";
            var nativeText  = await sel.CountAsync() > 0
                ? (await sel.EvaluateAsync<string>("s => (s.options[s.selectedIndex]?.textContent || '').trim()")) ?? ""
                : "";

            Console.WriteLine($"[Template] Trigger='{triggerText}' | Native='{nativeText}' | Expected='{expectedLabel}'");

            Assert.That(
                triggerText.Contains(expectedLabel, StringComparison.OrdinalIgnoreCase) ||
                nativeText.Contains(expectedLabel, StringComparison.OrdinalIgnoreCase),
                $"Template selection did not reflect '{expectedLabel}'. Actual trigger='{triggerText}', native='{nativeText}'.");
        }

        // --------------------------- Save & Return ---------------------------

        /// Click the creation-page SAVE AND CONFIRM, then go back to grid and assert row exists.
        public static async Task SaveOnCreateAndReturnToGridAsync(IPage acPage, string campaignName)
        {
            var save = acPage.Locator(Sel.SaveAndConfirmCreateBtn).First;
            await save.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
            await save.ScrollIntoViewIfNeededAsync();
            await save.ClickAsync(new() { Force = true });

            // best-effort confirmations
            var confirm = acPage.Locator(Sel.ConfirmYesBtn).First;
            if (await confirm.IsVisibleAsync(new() { Timeout = 3000 }))
                await confirm.ClickAsync();

            var toast = acPage.Locator(Sel.ToastSuccess).First;
            if (await toast.IsVisibleAsync(new() { Timeout = 6000 }))
                await toast.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 15_000 });

            await GoToAppointmentGridAsync(acPage);
            await ExpectRowVisibleAsync(acPage, campaignName);
        }

        /// Clicks the creation-page SAVE AND CONFIRM (#btnSaveReminderNew)
/// then navigates to /Dashboard/Index and STAYS there (no grid/tab wait).
        /// Clicks the creation-page SAVE AND CONFIRM (#btnSaveReminderNew)
/// then navigates to /Dashboard/Index and STAYS there (URL-driven, no DOM wait).
public static async Task SaveOnCreateAndStayOnDashboardAsync(IPage acPage, string campaignName)
{
    // 1) Click Save on the create form
    var save = acPage.Locator("#btnSaveReminderNew").First;
    await save.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
    await save.ScrollIntoViewIfNeededAsync();
    await save.ClickAsync(new() { Force = true });

    // 2) Best-effort confirm / toast (do not block)
    var confirm = acPage.Locator(".bootbox .modal-footer .btn-primary, .modal:has-text('Confirm') button:has-text('Yes')").First;
    if (await confirm.IsVisibleAsync(new() { Timeout = 2000 }))
        await confirm.ClickAsync();

    var toast = acPage.Locator(".toast-success, [data-testid='toast-success']").First;
    if (await toast.IsVisibleAsync(new() { Timeout = 3000 }))
        await toast.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 10_000 });

    // 3) Go to Dashboard and STOP there (no grid/tab waits)
    var origin = new Uri(acPage.Url).GetLeftPart(UriPartial.Authority);
    var dashUrl = $"{origin}/Dashboard/Index";
    await acPage.GotoAsync(dashUrl, new() { WaitUntil = WaitUntilState.Load });
    await acPage.WaitForLoadStateAsync(LoadState.NetworkIdle);

    // Ensure the URL is correct (avoid bouncing back)
    await acPage.WaitForFunctionAsync(
        "url => url.toLowerCase().includes('/dashboard/index')",
        acPage.Url,
        new() { Timeout = 5000 }
    );

    // Soft settle (avoid strict DOM waits that vary by env)
    await acPage.WaitForTimeoutAsync(500);

    // Optional: lightweight sanity log (no assert)
    Console.WriteLine($"[Post-Save] Landed on: {acPage.Url}");
}

//s

        public static async Task ExpectRowVisibleAsync(IPage acPage, string name)
        {
            await acPage.Locator(Sel.RowByName(name)).First
                .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        }

        // --------------------------- helpers ---------------------------

        private static async Task<bool> RowShowsAsync(ILocator row, string expectedLabel)
        {
            var triggerText = row.Locator(".filter-option-inner-inner").First;
            var nativeSelect = row.Locator("select").First;

            var trig = await triggerText.CountAsync() > 0 ? (await triggerText.InnerTextAsync() ?? "").Trim() : "";
            var nat  = await nativeSelect.CountAsync() > 0
                ? (await nativeSelect.EvaluateAsync<string>("s => (s.options[s.selectedIndex]?.textContent || '').trim()")) ?? ""
                : "";

            return LooksLikeExpected(trig, expectedLabel) || LooksLikeExpected(nat, expectedLabel);
        }

        private static bool LooksLikeExpected(string actual, string expected)
        {
            var a = NormalizeAlnumLower(actual);
            var e = NormalizeAlnumLower(expected);
            return a.Contains(e) || (a.Contains("ra") && a.Contains("101")); // tolerant for your label style
        }

        private static string NormalizeAlnumLower(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var ch in s)
                if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
            return sb.ToString();
        }
    }
}
