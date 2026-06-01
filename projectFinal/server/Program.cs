using System;
using System.IO;
using System.ServiceModel;
using BusinessLogic;
using Model;
using ViewDB;

namespace CheckersServer
{
    public static class Program
    {
        // Console host. Self-hosts the WCF service from App.config so
        // the same configuration controls bindings and endpoints, and
        // keeps the process alive until the user presses Enter.
        public static void Main()
        {
            Console.Title = "Checkers WCF Server";
            Console.WriteLine("===========================================");
            Console.WriteLine("  Checkers Game Server (WCF + WPF + Access)");
            Console.WriteLine("===========================================");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string accdb   = ResolveAccdbPath(baseDir);
            string schema  = ResolveSchemaPath(baseDir);

            try
            {
                Console.WriteLine("Database: " + accdb);
                DatabaseInitializer.EnsureDatabase(accdb, schema);
                Console.WriteLine("Database ready.");
                SeedDefaultAdminIfEmpty();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("ERROR: could not initialise the Access database.");
                Exception cur = ex;
                int depth = 0;
                while (cur != null)
                {
                    Console.WriteLine(new string(' ', depth * 2) +
                                      "[" + cur.GetType().Name + "] " + cur.Message);
                    cur = cur.InnerException;
                    depth++;
                }
                Console.WriteLine();
                Console.WriteLine("Process bitness: " + (Environment.Is64BitProcess ? "x64" : "x86"));
                Console.WriteLine("Expected DB at:  " + accdb);
                Console.WriteLine();
                Console.WriteLine("Press Enter to exit.");
                Console.ReadLine();
                return;
            }

            // Don't use a `using` block here: ServiceHost.Dispose calls
            // Close, which throws on a Faulted host. We need to manually
            // pick Abort vs Close depending on the host's state.
            ServiceHost host = new ServiceHost(typeof(CheckersService));
            try
            {
                host.Opened += (s, e) =>
                {
                    Console.WriteLine();
                    Console.WriteLine("Service is running. Endpoints:");
                    foreach (var ep in host.Description.Endpoints)
                        Console.WriteLine("  " + ep.Address.Uri + "  [" + ep.Binding.Name + "]");
                };
                host.Faulted += (s, e) =>
                    Console.WriteLine("[FAULT] Service host faulted.");
                    host.Open();

                Console.WriteLine();
                Console.WriteLine("Press Enter to stop the server.");
                Console.ReadLine();
            }
            finally
            {
                // Always release the host. Abort() is safe in any state,
                // including Faulted — it never throws.
                try
                {
                    if (host.State == CommunicationState.Opened) host.Close();
                    else                                         host.Abort();
                }
                catch { host.Abort(); }
            }
        }

        // Idempotent: makes sure an admin account with username "a" /
        // password "a" exists. Runs every startup but only inserts when
        // the row is missing, so it's safe on existing databases too.
        private static void SeedDefaultAdminIfEmpty()
        {
            UsersDB users = new UsersDB();
            User existing = users.GetByUsernameAsync("a").GetAwaiter().GetResult();
            if (existing != null)
            {
                // Already there — nothing to do. (We don't reset the
                // password here on purpose; if it's been changed, leave it.)
                return;
            }

            User admin = new User
            {
                Username     = "a",
                PasswordHash = PasswordHasher.Hash("a"),
                Email        = string.Empty,
                Role         = UserRole.Admin,
                Rating       = 1200,
                CreatedAt    = DateTime.Now
            };
            int newId = users.AddAsync(admin).GetAwaiter().GetResult();
            if (newId > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Default admin account is ready:");
                Console.WriteLine("    username: a");
                Console.WriteLine("    password: a");
            }
        }

        private static string ResolveAccdbPath(string baseDir)
        {
            DirectoryInfo dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 6 && dir != null; i++)
            {
                string viewDb = Path.Combine(dir.FullName, "ViewDB");
                if (Directory.Exists(viewDb))
                    return Path.Combine(viewDb, "ArchiveData3.accdb");
                dir = dir.Parent;
            }
            return Path.Combine(baseDir, "ArchiveData3.accdb");
        }

        private static string ResolveSchemaPath(string baseDir)
        {
            string near = Path.Combine(baseDir, "schema.sql");
            if (File.Exists(near)) return near;

            DirectoryInfo dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 6 && dir != null; i++)
            {
                string candidate = Path.Combine(dir.FullName, "ViewDB", "schema.sql");
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            return near;
        }
    }
}
