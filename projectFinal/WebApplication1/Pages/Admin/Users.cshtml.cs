using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.CheckersServer;
using WebApplication1.Services;

namespace WebApplication1.Pages.Admin;

public class UsersModel : PageModel
{
    private readonly CheckersBackend _backend;
    public UsersModel(CheckersBackend backend) { _backend = backend; }

    public List<User> Users { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    public string? Status { get; private set; }
    public bool IsError { get; private set; }

    public async Task OnGetAsync()
    {
        await ReloadAsync();
    }

    // Toggle ban / unban.
    public async Task<IActionResult> OnPostToggleBanAsync(int userId, bool ban)
    {
        var token = AdminAuth.RequireToken(User);
        try
        {
            bool ok = await _backend.AdminBanUserAsync(token, userId, ban);
            Status  = ok ? (ban ? "User banned." : "User unbanned.")
                          : "Action did not affect any rows.";
            IsError = !ok;
        }
        catch (Exception ex)
        {
            Status  = "Error: " + ex.Message;
            IsError = true;
        }
        await ReloadAsync();
        return Page();
    }

    // ----- view helpers -----

    public string Initials(string username)
    {
        if (string.IsNullOrEmpty(username)) return "?";
        var parts = username.Split(new[] { ' ', '_', '-', '.' },
                                   StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "?";
        if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpperInvariant();
        return (parts[0].Substring(0, 1) + parts[^1].Substring(0, 1)).ToUpperInvariant();
    }

    public string AvatarStyle(User u)
    {
        if (u.ProfilePicture != null && u.ProfilePicture.Length > 0)
        {
            string b64 = Convert.ToBase64String(u.ProfilePicture);
            return $"background-image:url('data:image/jpeg;base64,{b64}');";
        }
        // Deterministic colour from the username — matches the WPF client's
        // AvatarColorConverter palette so the same user looks the same in
        // both UIs.
        var palette = new[]
        {
            "#E57373", "#F06E4E", "#E5B14D", "#6FC276",
            "#4EB7C2", "#6699E5", "#9B77E5", "#E56EC2"
        };
        int hash = 0;
        foreach (char c in u.Username) hash = (hash * 31 + c) & 0x7FFFFFFF;
        return $"background-color:{palette[hash % palette.Length]};";
    }

    private async Task ReloadAsync()
    {
        var token = AdminAuth.RequireToken(User);
        try
        {
            var all = await _backend.AdminGetAllUsersAsync(token);
            if (!string.IsNullOrWhiteSpace(Query))
            {
                string q = Query.Trim();
                all = all.Where(u =>
                        u.Username.Contains(q, StringComparison.OrdinalIgnoreCase)
                        || (u.Email ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
                     .ToList();
            }
            Users = all;
        }
        catch (Exception ex)
        {
            Status  = "Could not load users: " + ex.Message;
            IsError = true;
        }
    }
}
