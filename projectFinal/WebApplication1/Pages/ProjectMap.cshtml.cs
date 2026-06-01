using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Pages;

// Public-facing project showcase. No authentication required — this is
// intentionally browseable so reviewers / classmates can land on the
// project URL and see the architecture without needing credentials.
[AllowAnonymous]
public class ProjectMapModel : PageModel
{
    public void OnGet() { }
}
