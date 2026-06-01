using System.Windows;
using CheckersClient.Services;
using CheckersClient.ViewModels;

namespace CheckersClient
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Single shared service-layer wrapper used by every page.
            CheckersServiceClient client = new CheckersServiceClient();

            AppViewModel root = new AppViewModel(client);
            MainWindow window = new MainWindow { DataContext = root };
            root.GoToLogin();
            window.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Best-effort logout so the server can clean up matchmaking
            // and online state.
            try { ((AppViewModel)MainWindow?.DataContext)?.OnAppExit(); }
            catch { /* swallow during shutdown */ }
            base.OnExit(e);
        }
    }
}
