using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Reflection;

namespace ViewDB
{
    // Ensures the Access database file exists and has the expected
    // schema. The .accdb is created via late-bound COM (ADOX, falling
    // back to DAO), then DDL from schema.sql is replayed statement by
    // statement.
    public static class DatabaseInitializer
    {
        public static void EnsureDatabase(string accdbPath, string schemaSqlPath)
        {
            BaseDB.Configure(accdbPath);

            if (!File.Exists(accdbPath))
            {
                CreateBlankAccessFile(accdbPath);
                ApplySchema(accdbPath, schemaSqlPath);
            }

            // For databases created by older versions of the project,
            // make sure the schema is up to date. Each migration is
            // idempotent — it inspects the current columns first.
            ApplyMigrations(accdbPath);
        }

        // Lightweight forward migration. Adds a column when it's missing,
        // never drops anything, never overwrites data. Easy to extend.
        private static void ApplyMigrations(string accdbPath)
        {
            string connStr =
                "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + accdbPath +
                ";Persist Security Info=False";

            using (OleDbConnection conn = new OleDbConnection(connStr))
            {
                conn.Open();
                EnsureColumn(conn, "Users", "ProfilePicture", "LONGBINARY");
                EnsureColumn(conn, "Users", "BirthDate",      "DATETIME");
                EnsureColumn(conn, "Users", "Country",        "TEXT(50)");
            }
        }

        private static void EnsureColumn(OleDbConnection conn, string table,
                                         string column, string type)
        {
            // OleDb's "Columns" schema collection lets us check whether a
            // column exists without parsing CREATE statements ourselves.
            System.Data.DataTable cols = conn.GetSchema("Columns",
                new[] { null, null, table, column });
            if (cols.Rows.Count > 0) return;

            using (OleDbCommand cmd = new OleDbCommand(
                "ALTER TABLE " + table + " ADD COLUMN " + column + " " + type, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        // Tries every Access COM API the system might have registered,
        // collecting diagnostic detail from every attempt so the user
        // sees something more useful than "Exception thrown by the
        // target of an invocation."
        private static void CreateBlankAccessFile(string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            List<string> attempts = new List<string>();

            // 1) ADOX with the ACE 12 provider (Office 2007+ runtime).
            if (TryAdox(path, "Microsoft.ACE.OLEDB.12.0", attempts)) return;
            // 2) ADOX with the ACE 16 provider (Office 2016+ runtime).
            if (TryAdox(path, "Microsoft.ACE.OLEDB.16.0", attempts)) return;
            // 3) DAO fallback (different ProgIDs across versions).
            if (TryDao(path, "DAO.DBEngine.120", attempts)) return;
            if (TryDao(path, "DAO.DBEngine.150", attempts)) return;

            throw new InvalidOperationException(BuildFailureMessage(attempts));
        }

        // ---- ADOX path ----

        private static bool TryAdox(string path, string provider, List<string> attempts)
        {
            Type catalogType = Type.GetTypeFromProgID("ADOX.Catalog");
            if (catalogType == null)
            {
                attempts.Add("ADOX.Catalog ProgID is not registered on this machine.");
                return false;
            }

            object catalog = null;
            try
            {
                catalog = Activator.CreateInstance(catalogType);
                string connStr = "Provider=" + provider + ";Data Source=" + path + ";";
                catalogType.InvokeMember(
                    "Create",
                    BindingFlags.InvokeMethod,
                    null, catalog, new object[] { connStr });
                return true;
            }
            catch (TargetInvocationException tex)
            {
                Exception inner = tex.InnerException ?? tex;
                attempts.Add("ADOX with " + provider + " → " +
                             inner.GetType().Name + ": " + inner.Message);
                return false;
            }
            catch (Exception ex)
            {
                attempts.Add("ADOX with " + provider + " → " +
                             ex.GetType().Name + ": " + ex.Message);
                return false;
            }
            finally
            {
                if (catalog != null)
                {
                    try { System.Runtime.InteropServices.Marshal.ReleaseComObject(catalog); }
                    catch { /* swallow */ }
                }
            }
        }

        // ---- DAO fallback ----

        private static bool TryDao(string path, string progId, List<string> attempts)
        {
            Type daoType = Type.GetTypeFromProgID(progId);
            if (daoType == null)
            {
                attempts.Add(progId + " ProgID is not registered.");
                return false;
            }

            object engine = null;
            try
            {
                engine = Activator.CreateInstance(daoType);
                // CreateDatabase(name, locale, options).
                // dbVersion150 = 128 — Access 2010+ (.accdb).
                daoType.InvokeMember(
                    "CreateDatabase",
                    BindingFlags.InvokeMethod,
                    null, engine,
                    new object[]
                    {
                        path,
                        ";LANGID=0x0409;CP=1252;COUNTRY=0",   // dbLangGeneral
                        128                                     // dbVersion150
                    });
                return true;
            }
            catch (TargetInvocationException tex)
            {
                Exception inner = tex.InnerException ?? tex;
                attempts.Add(progId + ".CreateDatabase → " +
                             inner.GetType().Name + ": " + inner.Message);
                return false;
            }
            catch (Exception ex)
            {
                attempts.Add(progId + ".CreateDatabase → " +
                             ex.GetType().Name + ": " + ex.Message);
                return false;
            }
            finally
            {
                if (engine != null)
                {
                    try { System.Runtime.InteropServices.Marshal.ReleaseComObject(engine); }
                    catch { /* swallow */ }
                }
            }
        }

        private static string BuildFailureMessage(List<string> attempts)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("Could not create the Access database. Tried:");
            foreach (string a in attempts) sb.Append("  • ").AppendLine(a);
            sb.AppendLine();
            sb.AppendLine("Likely causes:");
            sb.AppendLine("  1. Microsoft Access Database Engine is not installed.");
            sb.AppendLine("     Download \"Microsoft Access Database Engine 2016 Redistributable\"");
            sb.AppendLine("     and install the x86 version (this process is x86).");
            sb.AppendLine("  2. ACE is installed in a different bitness than this process.");
            sb.AppendLine("     If you have x64 Office installed, install the x86 redistributable");
            sb.AppendLine("     with /quiet (the GUI installer will refuse). The redistributable");
            sb.AppendLine("     can coexist with x64 Office.");
            sb.AppendLine("  3. Anti-virus is blocking COM activation.");
            return sb.ToString();
        }

        // ---- DDL replay ----

        private static void ApplySchema(string accdbPath, string schemaSqlPath)
        {
            if (!File.Exists(schemaSqlPath))
                throw new FileNotFoundException("schema.sql not found", schemaSqlPath);

            string text = File.ReadAllText(schemaSqlPath);
            List<string> statements = SplitStatements(text);

            string connStr =
                "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + accdbPath +
                ";Persist Security Info=False";

            using (OleDbConnection conn = new OleDbConnection(connStr))
            {
                conn.Open();
                foreach (string stmt in statements)
                {
                    string sql = stmt.Trim();
                    if (sql.Length == 0) continue;
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                        cmd.ExecuteNonQuery();
                }
            }
        }

        private static List<string> SplitStatements(string text)
        {
            List<string> result = new List<string>();
            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            System.Text.StringBuilder buf = new System.Text.StringBuilder();
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.StartsWith("--")) continue;
                buf.Append(' ').Append(line);
                if (line.EndsWith(";"))
                {
                    string s = buf.ToString().Trim().TrimEnd(';').Trim();
                    if (s.Length > 0) result.Add(s);
                    buf.Clear();
                }
            }
            string tail = buf.ToString().Trim().TrimEnd(';').Trim();
            if (tail.Length > 0) result.Add(tail);
            return result;
        }
    }
}
