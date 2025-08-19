using Microsoft.Playwright;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Microsoft.Playwright.Assertions;

namespace Enosis.QA.Automation.Tests.Flows
{
    /// <summary>
    /// Flow helpers for the Mailing List UI inside Automated Campaigns.
    /// </summary>
    public static class MailingListFlow
    {
        private static readonly Regex MailingListUrl = new(@"/mailinglist(?:/index)?", RegexOptions.IgnoreCase);
        private static readonly Regex AddCustomLabel = new(@"^\s*(\+?\s*)?Add\s+Custom\s+Mail(?:ing)?\s+List\s*$", RegexOptions.IgnoreCase);

        /// <summary>
        /// From the AC window: open the left-nav “Mailing List” and click “Add Custom Mailing List”.
        /// </summary>
        public static async Task OpenAndClickAddAsync(IPage acPage)
        {
            await acPage.BringToFrontAsync();

            // Click the left-nav “Mailing List”
            ILocator mlLink = acPage.Locator("a[href='/mailingList/Index']").First;
            if (await mlLink.CountAsync() == 0)
                mlLink = acPage.Locator("a:has(> span.tab-title:has-text('Mailing List'))").First;
            if (await mlLink.CountAsync() == 0)
                mlLink = acPage.GetByRole(AriaRole.Link,
                    new() { NameRegex = new Regex(@"^\s*Mailing\s*List\s*$", RegexOptions.IgnoreCase) });

            bool onMailingList = MailingListUrl.IsMatch(acPage.Url);

            if (!onMailingList && await mlLink.CountAsync() > 0)
            {
                try
                {
                    var waitUrl = acPage.WaitForURLAsync(MailingListUrl, new() { Timeout = 10_000 });
                    await Task.WhenAll(waitUrl, mlLink.ClickAsync(new() { Force = true }));
                    onMailingList = true;
                }
                catch
                {
                    try
                    {
                        var waitUrl = acPage.WaitForURLAsync(MailingListUrl, new() { Timeout = 7_000 });
                        await mlLink.EvaluateAsync("el => el.click()");
                        await waitUrl;
                        onMailingList = true;
                    }
                    catch { /* fall back to direct nav */ }
                }
            }

            // Direct nav fallback
            if (!onMailingList)
            {
                var origin = new Uri(acPage.Url).GetLeftPart(UriPartial.Authority);
                var dest   = new Uri(new Uri(origin), "/mailingList/Index").ToString();
                await acPage.GotoAsync(dest, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
            }

            // Find the “Add Custom mailing List” control (button or link; page or iframe)
            ILocator addBtn = acPage.GetByRole(AriaRole.Button, new() { NameRegex = AddCustomLabel });
            if (await addBtn.CountAsync() == 0)
                addBtn = acPage.GetByRole(AriaRole.Link, new() { NameRegex = AddCustomLabel });
            if (await addBtn.CountAsync() == 0)
                addBtn = acPage.Locator("button.btn-add-mail-list, a.btn-add-mail-list, button:has-text('Add Custom'), a:has-text('Add Custom')");

            if (await addBtn.CountAsync() == 0)
            {
                var mlFrame = acPage.Frames.FirstOrDefault(f => MailingListUrl.IsMatch(f.Url ?? ""));
                if (mlFrame != null)
                {
                    addBtn = mlFrame.GetByRole(AriaRole.Button, new() { NameRegex = AddCustomLabel });
                    if (await addBtn.CountAsync() == 0)
                        addBtn = mlFrame.GetByRole(AriaRole.Link, new() { NameRegex = AddCustomLabel });
                    if (await addBtn.CountAsync() == 0)
                        addBtn = mlFrame.Locator("button.btn-add-mail-list, a.btn-add-mail-list, button:has-text('Add Custom'), a:has-text('Add Custom')");
                }
            }

            await addBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 120_000 });
            await addBtn.ScrollIntoViewIfNeededAsync();
            try { await addBtn.ClickAsync(new() { Force = true }); }
            catch { await addBtn.EvaluateAsync("el => el.click()"); }
        }

        /// <summary>
        /// Fill “Name of Custom Mailing List” and click the Office/Group “Select” button.
        /// </summary>
        public static async Task FillNameAndOpenOfficeSelectAsync(IPage acPage, string listName)
        {
            await acPage.BringToFrontAsync();

            ILocator nameInput = acPage.Locator("#MAILINGLISTNAME");
            ILocator selectBtn = acPage.Locator("#btnOfficesSelectionMailingList");

            // Modal dialog?
            if (await nameInput.CountAsync() == 0)
            {
                var dlg = acPage.GetByRole(AriaRole.Dialog);
                if (await dlg.CountAsync() > 0)
                {
                    nameInput = dlg.Locator("#MAILINGLISTNAME");
                    selectBtn = dlg.Locator("#btnOfficesSelectionMailingList");
                }
            }

            // Iframe?
            if (await nameInput.CountAsync() == 0)
            {
                foreach (var frame in acPage.Frames)
                {
                    if (await frame.Locator("#MAILINGLISTNAME").CountAsync() > 0)
                    {
                        nameInput = frame.Locator("#MAILINGLISTNAME");
                        selectBtn = frame.Locator("#btnOfficesSelectionMailingList");
                        break;
                    }
                }
            }

            await nameInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 120_000 });
            await nameInput.FillAsync(listName);

            await selectBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 120_000 });
            await selectBtn.ScrollIntoViewIfNeededAsync();
            try { await selectBtn.ClickAsync(new() { Force = true }); }
            catch { await selectBtn.EvaluateAsync("el => el.click()"); }
        }

        /// <summary>
        /// In the “Select Office” modal: search, pick office, move right, and Apply.
        /// </summary>
        public static async Task SelectOfficeBySearchAndApplyAsync(
            IPage acPage,
            string searchText,
            string officeId = "101",
            string officeLabel = "Organic Dentistry (101)")
        {
            await acPage.BringToFrontAsync();

            var modal = acPage.Locator(".modal-dialog:has-text('SELECT OFFICE'), [role='dialog']:has-text('SELECT OFFICE')").First;
            await modal.WaitForAsync(new() { Timeout = 120_000 });

            // Search box (left panel)
            var search = modal.Locator("input.search-left-panel").First;
            await search.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 120_000 });
            await search.ClickAsync(new() { Force = true });
            await search.PressAsync("Control+A");
            await search.PressAsync("Delete");
            await search.FillAsync(searchText);

            // Debounce for client-side filtering
            await acPage.WaitForTimeoutAsync(300);

            // Left select (Available Office)
            var leftSelect = modal.Locator("select").First;
            bool selected = false;

            // Prefer native selectOption by label
            try
            {
                await leftSelect.SelectOptionAsync(new[] { new SelectOptionValue { Label = officeLabel } });
                selected = true;
            }
            catch { /* fallbacks next */ }

            if (!selected)
            {
                // Fallback: click the <option> by data-id or label
                var byId = modal.Locator($"option[data-id='{officeId}']").First;
                if (await byId.CountAsync() > 0)
                {
                    await byId.ScrollIntoViewIfNeededAsync();
                    await byId.ClickAsync();
                    selected = true;
                }
                else
                {
                    var byLabel = modal.Locator("option", new() { HasTextString = officeLabel }).First;
                    await byLabel.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 120_000 });
                    await byLabel.ScrollIntoViewIfNeededAsync();
                    await byLabel.ClickAsync();
                    selected = true;
                }
            }

            // Move to right
            var moveRight = modal.Locator("button.moveToRight").First;
            await moveRight.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 120_000 });
            await moveRight.ClickAsync();

            // Confirm it appears in the right select, then Apply
            var rightSelect = modal.Locator("select").Nth(1);
            await Expect(rightSelect.Locator("option", new() { HasTextString = officeLabel })).ToBeVisibleAsync();

            var apply = modal.Locator("#btnSelectOffices");
            await apply.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 120_000 });
            await apply.ClickAsync();
        }

        /// <summary>
        /// Click “SAVE AND CONFIRM”, wait for modal close and confirm the new list appears.
        /// </summary>
        public static async Task SaveAndConfirmAsync(IPage acPage, string listName, int appearTimeoutMs = 8000)
{
    await acPage.BringToFrontAsync();

    // 1) Pick ONE modal element (id first, then named dialog). Avoid comma selectors in strict mode.
    ILocator modalRoot = acPage.Locator("#addEditmailingListModal"); // unique id
    if (await modalRoot.CountAsync() == 0)
    {
        modalRoot = acPage.GetByRole(
            AriaRole.Dialog,
            new() { NameRegex = new System.Text.RegularExpressions.Regex(@"\bAdd\s+Custom\s+Mail", System.Text.RegularExpressions.RegexOptions.IgnoreCase) }
        ).First;
    }

    // Wait for the modal to be present/visible (short)
    await modalRoot.WaitForAsync(new() { Timeout = 30_000, State = WaitForSelectorState.Visible });

    // 2) Click SAVE AND CONFIRM (scoped to the same modal)
    var saveBtn = modalRoot.Locator("#btnAEMailingList").First;
    await saveBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
    await saveBtn.ScrollIntoViewIfNeededAsync();
    try { await saveBtn.ClickAsync(new() { Force = true }); }
    catch { await saveBtn.EvaluateAsync("el => el.click()"); }

    // 3) Modal close: many apps HIDE the modal instead of removing it.
    // Prefer waiting for the save button to hide; fallback to modal hidden.
    bool closed = false;
    try
    {
        await Microsoft.Playwright.Assertions.Expect(saveBtn).ToBeHiddenAsync(new() { Timeout = 8_000 });
        closed = true;
    }
    catch
    {
        try
        {
            await Microsoft.Playwright.Assertions.Expect(modalRoot).ToBeHiddenAsync(new() { Timeout = 8_000 });
            closed = true;
        }
        catch { /* try close X below */ }
    }

    // If still visible, try clicking the "X" once, then assert hidden
    if (!closed && await modalRoot.IsVisibleAsync())
    {
        var closeX = modalRoot.Locator("button.close, .modal-header .close, [data-dismiss='modal'], .fa-times, .fa-close").First;
        if (await closeX.CountAsync() > 0)
        {
            await closeX.ClickAsync(new() { Force = true });
            await Microsoft.Playwright.Assertions.Expect(modalRoot).ToBeHiddenAsync(new() { Timeout = 10_000 });
        }
    }

    // 4) Verify the new list appears quickly in the left panel (no long idle waits)
    var newItem = acPage.Locator("a#hrfmailinglist").Filter(new() { HasTextString = listName }).First;
    await Microsoft.Playwright.Assertions.Expect(newItem).ToBeVisibleAsync(new() { Timeout = appearTimeoutMs });

    // tiny pause so you can see it in headed runs
    await acPage.WaitForTimeoutAsync(100);
}



        /// <summary>
        /// Search for a mailing list by name and delete it (handles confirm dialog).
        /// </summary>
public static async Task DeleteMailingListAsync(IPage acPage, string listName)
{
    await acPage.BringToFrontAsync();

    // --- Filter + select the item ---
    var searchBox = acPage.Locator("#txtSearchMailingList");
    await searchBox.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
    await searchBox.ClickAsync();
    await searchBox.PressAsync("Control+A");
    await searchBox.PressAsync("Delete");
    await searchBox.FillAsync(listName);
    await acPage.Keyboard.PressAsync("Enter");
    await acPage.WaitForTimeoutAsync(250);

    var item = acPage.Locator("a#hrfmailinglist").Filter(new() { HasTextString = listName }).First;
    await item.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
    await item.ScrollIntoViewIfNeededAsync();
    await item.ClickAsync();
    await acPage.WaitForTimeoutAsync(150);

    // Make sure the item shows as selected (has 'active'); retry click if needed.
    var becameActive = false;
    for (int i = 0; i < 5 && !becameActive; i++)
    {
        try { becameActive = await item.EvaluateAsync<bool>("el => el.classList && el.classList.contains('active')"); }
        catch { becameActive = false; }
        if (!becameActive)
        {
            await item.ClickAsync();
            await acPage.WaitForTimeoutAsync(150);
        }
    }

    // --- Delete must be enabled ---
    var deleteBtn = acPage.Locator("#btnDeleteMailingList").First;
    await deleteBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
    await Microsoft.Playwright.Assertions.Expect(deleteBtn).ToBeEnabledAsync(new() { Timeout = 10_000 });
    await deleteBtn.ScrollIntoViewIfNeededAsync();

    // --- Click Delete and wait for a VISIBLE Bootbox confirm (retry a few times) ---
    // OK button strictly within a visible Bootbox modal
    var okBtn = acPage.Locator(
        ":is(.bootbox.modal.in, .bootbox.modal.show) button[data-bb-handler='confirm'], " +
        ":is(.bootbox.modal.in, .bootbox.modal.show) .modal-footer .btn-primary:has-text('OK')"
    ).First;

    var appeared = false;
    for (int attempt = 0; attempt < 4 && !appeared; attempt++)
    {
        await deleteBtn.ClickAsync();                       // normal click
        try
        {
            await okBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 3_000 });
            appeared = true;
        }
        catch
        {
            // one more try with a forced click after a short wait
            await acPage.WaitForTimeoutAsync(300);
            await deleteBtn.ClickAsync(new() { Force = true });
            try
            {
                await okBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 3_000 });
                appeared = true;
            }
            catch { /* loop and retry */ }
        }
    }

    if (!appeared)
        throw new TimeoutException("Delete confirm dialog did not appear after clicking Delete.");

    // --- Click OK inside the dialog and wait for it to disappear ---
    await okBtn.ScrollIntoViewIfNeededAsync();
    await okBtn.ClickAsync(new() { Force = true });

    // Wait for any visible Bootbox to go away
    var visibleBootbox = acPage.Locator(":is(.bootbox.modal.in, .bootbox.modal.show)").First;
    try { await Microsoft.Playwright.Assertions.Expect(visibleBootbox).ToBeHiddenAsync(new() { Timeout = 8_000 }); }
    catch { /* sometimes fades fast; continue */ }

    // --- Assert the item is gone (re-apply filter once if needed) ---
    try
    {
        await Microsoft.Playwright.Assertions.Expect(item).Not.ToBeVisibleAsync(new() { Timeout = 8_000 });
    }
    catch
    {
        await searchBox.ClickAsync();
        await searchBox.PressAsync("Control+A");
        await searchBox.PressAsync("Delete");
        await searchBox.FillAsync(listName);
        await acPage.Keyboard.PressAsync("Enter");

        await Microsoft.Playwright.Assertions.Expect(
            acPage.Locator("a#hrfmailinglist").Filter(new() { HasTextString = listName }).First
        ).Not.ToBeVisibleAsync(new() { Timeout = 8_000 });
    }

    await acPage.WaitForTimeoutAsync(100);
}


    }
}
