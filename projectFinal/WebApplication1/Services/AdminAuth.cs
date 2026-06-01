using System.Security.Claims;

namespace WebApplication1.Services;

// Tiny helper that pulls the WCF session token off the cookie's
// claims. Pages call AdminAuth.RequireToken(User) at the start of
// each handler — fails fast if the user somehow isn't authenticated
// (the [AdminOnly] policy should already have rejected them by then).
public static class AdminAuth
{
    public const string TokenClaim = "WcfToken";

    public static Guid RequireToken(ClaimsPrincipal user)
    {
        string? raw = user.FindFirst(TokenClaim)?.Value;
        if (Guid.TryParse(raw, out var token)) return token;
        throw new InvalidOperationException(
            "Admin session is missing the WCF token claim — sign out and sign back in.");
    }
}
