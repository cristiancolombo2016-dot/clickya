using ClickYa.Comercios.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClickYa.Comercios.Pages;

public sealed class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        await HttpContext.SignOutAsync(WebSecurity.CookieScheme);
        return RedirectToPage("/Index");
    }
}
