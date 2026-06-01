using System;
using System.IO;
using BusinessLogic;
using Model;
using ViewDB;

namespace CheckersServer
{
    // Initialisation that used to live in Program.Main now runs from
    // a static class, so the same setup happens whether the assembly
    // is hosted by:
    //   • WcfSvcHost / WcfTestClient (when running as a WCF Service
    //     Library — the default school-template behaviour),
    //   • IIS,
    //   • or a custom console host (Program.Main, kept around as a
    //     fallback for command-line scenarios).
    //
    // EnsureInitialised is idempotent: the first caller does the work,
    // every subsequent caller is a no-op. CheckersService's static
    // constructor calls it, which guarantees the DB is ready before
    // WCF dispatches the first operation.
    internal static class Bootstrap
    {
        private static readonly object _lock = new object();
        private static bool _ran;

        public static void EnsureInitialised()
        {
            lock (_lock)
            {
                if (_ran) return;
                _ran = true;

                string baseDir = AssemblyDirectory();
                string accdb   = LocateAccdbPath(baseDir);
                string schema  = LocateSchemaPath(baseDir);

                DatabaseInitializer.EnsureDatabase(accdb, schema);
                SeedDefaultAdmin();
                // Make sure the in-process bot opponent exists.
                CheckersService.Manager.EnsureBotUserAsync().GetAwaiter().GetResult();
            }
        }

        // The path to the directory holding server.dll. WcfSvcHost
        // launches us from its own folder, so AppDomain.BaseDirectory
        // is wrong — we need the location of THIS assembly.
        private static string AssemblyDirectory()
        {
            return Path.GetDirectoryName(typeof(Bootstrap).Assembly.Location);
        }

        // Idempotent — only inserts when the row is missing.
        private static void SeedDefaultAdmin()
        {
            UsersDB users = new UsersDB();
            User existing = users.GetByUsernameAsync("a").GetAwaiter().GetResult();
            if (existing != null) return;

            users.AddAsync(new User
            {
                Username     = "a",
                PasswordHash = PasswordHasher.Hash("a"),
                Email        = string.Empty,
                Role         = UserRole.Admin,
                Rating       = 1200,
                CreatedAt    = DateTime.Now
            }).GetAwaiter().GetResult();
        }

        private static string LocateAccdbPath(string baseDir)
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

        private static string LocateSchemaPath(string baseDir)
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
