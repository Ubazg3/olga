using CheckersMaui.Services.CheckersServer;

namespace CheckersMaui.Services;

// Process-wide session state. Static because:
//  * the app has at most one logged-in user at a time
//  * passing it through every page constructor would be noise
//  * the school's reference uses the same pattern
public static class AppState
{
    // Auth.
    public static Guid    Token    { get; set; } = Guid.Empty;
    public static User?   User     { get; set; }
    public static bool    IsLoggedIn => Token != Guid.Empty && User != null;

    // Convenience accessors.
    public static int     UserId   => User?.Id ?? 0;
    public static string  Username => User?.Username ?? string.Empty;
    public static bool    IsAdmin  => User?.Role == UserRole.Admin;

    public static void Clear()
    {
        Token = Guid.Empty;
        User  = null;
    }
}
