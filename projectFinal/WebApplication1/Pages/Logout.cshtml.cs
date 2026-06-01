using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Services;

namespace WebApplication1.Pages;

public class LogoutModel : PageModel
{
    private readonly CheckersBackend _backend;
    public LogoutModel(CheckersBackend backend) { _backend = backend; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Tell the WCF server to drop the session, then clear the cookie.
        var tokenStr = User.FindFirst("WcfToken")?.Value;
        if (Guid.TryParse(tokenStr, out var token))
        {
            try { await _backend.LogoutAsync(token); } catch { /* swallow */ }
        }
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Login");
    }
}
