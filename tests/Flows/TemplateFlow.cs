// ========================= tests/Flows/TemplateFlow.cs =========================
// Templates navigation + "Add New Template" click with robust fallbacks and retries.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Enosis.QA.Automation.Tests.Flows
{
    public static class TemplateFlow
    {
        public static async Task<IPage> OpenTemplatesAsync(IPage acPage)
        {
            await acPage.BringToFrontAsync();

            var link = acPage.Locator("a#template-button").First;
            var altLink = acPage.Locator("a[href*='/template/Index'], a[href*='/template/index']").First;
            var icon = acPage.Locator("a#template-button .side-menu-icon.template-icon").First;
            var leftNav = acPage.Locator(".left-sidemenu, .side-menu, .sidebar").First;

            if (await link.CountAsync() == 0 && await altLink.CountAsync() == 0)
                await acPage.WaitForSelectorAsync("a#template-button, a[href*='/template/Index'], a[href*='/template/index']", new() { Timeout = 15000 });

            if (await link.CountAsync() == 0) link = altLink;

            if (await leftNav.CountAsync() > 0)
                await leftNav.HoverAsync();

            // 1) Normal click
            try
            {
                await link.ScrollIntoViewIfNeededAsync();
                await link.ClickAsync(new() { Force = true });
                await acPage.WaitForURLAsync(new Regex("/template/index", RegexOptions.IgnoreCase), new() { Timeout = 8000 });
                return acPage;
            }
            catch { }

            // 2) Click icon inside item
            try
            {
                if (await icon.CountAsync() > 0)
                {
                    await icon.ScrollIntoViewIfNeededAsync();
                    await icon.ClickAsync(new() { Force = true });
                    await acPage.WaitForURLAsync(new Regex("/template/index", RegexOptions.IgnoreCase), new() { Timeout = 8000 });
                    return acPage;
                }
            }
            catch { }

            // 3) Mouse by coordinates (keep pointer over the nav to prevent collapse)
            try
            {
                var box = await link.BoundingBoxAsync();
                if (box is not null)
                {
                    if (await leftNav.CountAsync() > 0)
                    {
                        var nb = await leftNav.BoundingBoxAsync();
                        if (nb is not null)
                            await acPage.Mouse.MoveAsync(nb.X + 5, nb.Y + nb.Height / 2);
                    }
                    await acPage.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
                    await acPage.Mouse.DownAsync();
                    await acPage.Mouse.UpAsync();
                    await acPage.WaitForURLAsync(new Regex("/template/index", RegexOptions.IgnoreCase), new() { Timeout = 8000 });
                    return acPage;
                }
            }
            catch { }

            // 4) JS click
            try
            {
                var handle = await link.ElementHandleAsync();
                if (handle is not null)
                {
                    await acPage.EvaluateAsync("el => el.click()", handle);
                    await acPage.WaitForURLAsync(new Regex("/template/index", RegexOptions.IgnoreCase), new() { Timeout = 8000 });
                    return acPage;
                }
            }
            catch { }

            // 5) Navigate by href
            string? href = null;
            if (await link.CountAsync() > 0) href = await link.GetAttributeAsync("href");
            else if (await altLink.CountAsync() > 0) href = await altLink.GetAttributeAsync("href");

            if (!string.IsNullOrWhiteSpace(href))
            {
                if (!href!.StartsWith("http"))
                {
                    var origin = new System.Uri(acPage.Url).GetLeftPart(System.UriPartial.Authority);
                    if (!href.StartsWith("/")) href = "/" + href;
                    href = origin + href;
                }
                await acPage.GotoAsync(href!, new() { WaitUntil = WaitUntilState.Load });
                await acPage.WaitForURLAsync(new Regex("/template/index", RegexOptions.IgnoreCase), new() { Timeout = 10000 });
                return acPage;
            }

            throw new PlaywrightException("Failed to open Templates: could not activate the Templates link.");
        }

        /// Clicks "Add New Template" and waits for a modal or create/edit view.
        public static async Task ClickAddNewTemplateAsync(IPage page)
        {
            await page.WaitForURLAsync("**/template/Index");

            var btn = page.Locator("#btnAddNewTemplate").First;
            await btn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

            // Helper to decide if the click "worked"
            async Task<bool> CreationUiAppearedAsync()
            {
                // any modal
                if (await page.Locator("#addTemplateModal, .modal.show, .modal[role='dialog'][style*='display: block']").First
                                 .IsVisibleAsync(new() { Timeout = 500 }))
                    return true;

                // navigated to add/create
                try
                {
                    await page.WaitForURLAsync(new Regex("/template/(add|create|new|edit)", RegexOptions.IgnoreCase),
                                               new() { Timeout = 500 });
                    return true;
                }
                catch { }

                // core form controls visible
                if (await page.Locator("#TemplateName, input[name='TemplateName'], textarea[name='Body'], #btnSaveTemplate")
                              .First.IsVisibleAsync(new() { Timeout = 500 }))
                    return true;

                return false;
            }

            // Try once quickly
            try { await btn.ClickAsync(new() { Timeout = 1200 }); }
            catch { }

            if (await CreationUiAppearedAsync()) return;

            // Scroll & force click
            try
            {
                await page.EvaluateAsync("() => window.scrollTo(0, document.body.scrollHeight)");
                await page.EvaluateAsync(@"() => {
                    const scrollers = document.querySelectorAll('.slimScrollDiv, .left-sidemenu, .side-menu, .sidebar, .main-section');
                    scrollers.forEach(el => { try { el.scrollTop = el.scrollHeight; } catch {} });
                }");
                var h = await btn.ElementHandleAsync();
                if (h is not null) await page.EvaluateAsync("el => el.scrollIntoView({block:'center', inline:'center'})", h);

                await btn.ClickAsync(new() { Timeout = 2000, Force = true });
            }
            catch { }

            if (await CreationUiAppearedAsync()) return;

            // Mouse coords
            try
            {
                var box = await btn.BoundingBoxAsync();
                if (box is not null)
                {
                    await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
                    await page.Mouse.DownAsync();
                    await page.Mouse.UpAsync();
                }
            }
            catch { }

            if (await CreationUiAppearedAsync()) return;

            // JS click
            try
            {
                var handle = await btn.ElementHandleAsync();
                if (handle is not null)
                    await page.EvaluateAsync("el => el.click()", handle);
            }
            catch { }

            if (await CreationUiAppearedAsync()) return;

            // Final: small retry loop (forms sometimes swallow the first click)
            for (int i = 0; i < 3; i++)
            {
                await page.WaitForTimeoutAsync(500);
                try { await btn.ClickAsync(new() { Timeout = 800, Force = true }); } catch { }
                if (await CreationUiAppearedAsync()) return;
            }

            throw new PlaywrightException("Could not activate 'Add New Template' (no modal or create view detected).");
        }

        public static async Task CopyFromTemplateAsync(IPage page, string templateLabel)
        {
            // Modal root
            var modal = page.Locator("div[role='dialog'], .modal.show").First;
            await modal.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });

            // The native <select> (Copy Template)
            var select = modal.Locator("#ddlCopyTemplate").First;
            await select.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 8000 });

            // Give the user-visible effect (open the dropdown), then select by label
            await select.ClickAsync(); // opens the native list UI
            try
            {
                await select.SelectOptionAsync(new[] { new SelectOptionValue { Label = templateLabel } });
            }
            catch
            {
                // Fallback: set the option by JS + fire change
                var ok = await page.EvaluateAsync<bool>(@"(label) => {
            const s = document.querySelector('#ddlCopyTemplate');
            if (!s) return false;
            const opt = [...s.options].find(o => (o.textContent||'').trim() === label);
            if (!opt) return false;
            s.value = opt.value;
            s.dispatchEvent(new Event('change', { bubbles: true }));
            return true;
        }", templateLabel);
                if (!ok) throw new PlaywrightException($"Template '{templateLabel}' not found in Copy Template list.");
            }

            // Verify selection really changed to the requested template
            var selectedText = await page.EvaluateAsync<string>(@"
        (() => {
            const s = document.querySelector('#ddlCopyTemplate');
            return s ? (s.options[s.selectedIndex]?.textContent || '').trim() : '';
        })()");
            if (!string.Equals(selectedText, templateLabel, StringComparison.Ordinal))
                throw new PlaywrightException($"Copy Template selection mismatch. Expected '{templateLabel}', got '{selectedText}'.");
        }

        public static async Task SaveTemplateAsync(IPage page, string templateName)
        {
            // Small wait to allow template details to load after copying
            await page.WaitForTimeoutAsync(2000); // 2s buffer, adjust if needed

            // Wait for Name field
            var nameInput = page.Locator("#txtTemplateName");
            await nameInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });

            // Fill the template name
            await nameInput.FillAsync(templateName);

            // Click SAVE
            var saveButton = page.Locator("#btnSaveTemplate");
            await saveButton.ClickAsync();

            // Wait for network to settle (optional)
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        public static async Task DeleteTemplateByNameAsync(IPage page, string templateName)
        {
            // Selectors (stable + close to your markup)
            const string SearchBox          = "#txtSearchTemplatesList";
            static string ListItem(string name)
                => $".list-group a.list-group-item:has-text('{name}')";
            const string DeleteBtn          = "#btnDeleteTemplate, button#btnDeleteTemplate.btn-delete-office";
            const string BootboxModal       = ".bootbox.modal, .modal.in.bootbox, .bootbox .modal-dialog";
            const string BootboxConfirmBtn  = ".bootbox .btn-primary, .bootbox .modal-footer .btn-primary, button[data-bb-handler='confirm']";
            const string ToastSuccess       = ".toast-success, [data-testid='toast-success']";

            // 1) Search for the template
            var search = page.Locator(SearchBox).First;
            await search.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
            await search.FillAsync(string.Empty);
            await search.TypeAsync(templateName);
            await search.PressAsync("Enter");                    // some lists filter on Enter

            // Give the list a moment to refresh
            await page.WaitForTimeoutAsync(500);

            // 2) Click the template in the list
            var item = page.Locator(ListItem(templateName)).First;
            await item.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
            await item.ClickAsync(new() { Force = true });

            // 3) Click "DELETE"
            var delete = page.Locator(DeleteBtn).First;
            await delete.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
            await delete.ClickAsync();

            // 4) Confirm in the Bootbox dialog
            var modal = page.Locator(BootboxModal).First;
            await modal.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });

            var ok = page.Locator(BootboxConfirmBtn).First;
            await ok.ClickAsync();

            // 5) Success signal: toast disappears OR the item vanishes from the list
            var toast = page.Locator(ToastSuccess).First;
            try
            {
                // Wait for toast show -> hide (best signal if available)
                await toast.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 8_000 });
                await toast.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 15_000 });
            }
            catch
            {
                // Fallback: ensure the list item is gone
                await item.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 15_000 });
            }
        }





    }
}

    
    

