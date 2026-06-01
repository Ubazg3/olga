using System.ServiceModel;
using CheckersMaui.Services.CheckersServer;

namespace CheckersMaui.Services;

// Thin wrapper over the WCF proxy:
//   * builds a fresh BasicHttp client per call
//   * cleans it up regardless of success / failure
//
// Each request creates a new channel — the school's reference does
// the same, and it dovetails with our stateless server design (every
// call carries the auth token, no WCF session is needed).
public static class ServiceHelper
{
    // ----- IMPORTANT -----
    // The desktop side runs the server at:
    //   http://localhost:8733/Design_Time_Addresses/CheckersService/
    //
    // Replace the host below with the dev machine's LAN IP when
    // running on a real Android device or a different host on the
    // same network. Pick from the right line:
    //
    //   * Windows / Mac Catalyst test build:           localhost
    //   * Android emulator (AVD) on the same machine:  10.0.2.2
    //   * Real Android phone over Wi-Fi:               <PC's LAN IP>
    //
    // `ipconfig` / `ifconfig` will show the LAN IP. The phone must be
    // on the same Wi-Fi network as the PC.
    public const string ServiceHost = "10.0.2.2";

    public const string ServiceUrl =
        "http://" + ServiceHost + ":8733/Design_Time_Addresses/CheckersService/";

    public static CheckersServiceClient GetClient()
    {
        var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
        {
            MaxReceivedMessageSize = 5_000_000,
            OpenTimeout    = TimeSpan.FromSeconds(15),
            SendTimeout    = TimeSpan.FromSeconds(30),
            ReceiveTimeout = TimeSpan.FromSeconds(30),
        };
        return new CheckersServiceClient(binding, new EndpointAddress(ServiceUrl));
    }

    public static async Task<T> CallAsync<T>(Func<CheckersServiceClient, Task<T>> action)
    {
        var c = GetClient();
        try { return await action(c); }
        finally { await SafeCloseAsync(c); }
    }

    public static async Task CallAsync(Func<CheckersServiceClient, Task> action)
    {
        var c = GetClient();
        try { await action(c); }
        finally { await SafeCloseAsync(c); }
    }

    private static async Task SafeCloseAsync(CheckersServiceClient c)
    {
        try { await c.CloseAsync(); }
        catch { c.Abort(); }
    }
}
