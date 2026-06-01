using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.CheckersServer;
using WebApplication1.Services;

namespace WebApplication1.Pages;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly CheckersBackend _backend;
    public LoginModel(CheckersBackend backend) { _backend = backend; }

    [BindProperty] public string Username { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";

    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Username and password are required.";
            return Page();
        }

        LoginResult? res;
        try
        {
            res = await _backend.LoginAsync(Username.Trim(), Password);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Could not reach the game server: " + ex.Message;
            return Page();
        }

        if (res == null || !res.Success || res.User == null)
        {
            ErrorMessage = res?.Error ?? "Sign-in failed.";
            return Page();
        }

        if (res.User.Role != UserRole.Admin)
        {
            // Regular players belong on the desktop client. Cleanly tell
            // the server we're done so the matchmaking slot frees.
            try { await _backend.LogoutAsync(res.Token); } catch { }
            ErrorMessage = "This console is for admin accounts only.";
            return Page();
        }

        // Stash the WCF session token on the cookie so admin pages can
        // call admin-only operations without prompting again.
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name,           res.User.Username),
            new(ClaimTypes.NameIdentifier, res.User.Id.ToString()),
            new(ClaimTypes.Role,           "Admin"),
            new("WcfToken",                res.Token.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToPage("/Admin/Dashboard");
    }
}
