using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.CheckersServer;
using WebApplication1.Services;

namespace WebApplication1.Pages.Admin;

public class DashboardModel : PageModel
{
    private readonly CheckersBackend _backend;
    public DashboardModel(CheckersBackend backend) { _backend = backend; }

    public ServerStatsDto? Stats { get; private set; }
    public string? Error { get; private set; }

    public async Task OnGetAsync()
    {
        var token = AdminAuth.RequireToken(User);
        try
        {
            Stats = await _backend.AdminGetServerStatsAsync(token);
        }
        catch (Exception ex)
        {
            Error = "Could not reach the game server: " + ex.Message;
        }
    }
}
