using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Threading.Tasks;
using Model;

namespace ViewDB
{
    // Generic Access database access. Each public CRUD method opens a
    // fresh OleDbConnection so concurrent callers (multiple game sessions
    // using the WCF service) do not stomp on each other's connection
    // state. All SQL is parameterised — no string concatenation of user
    // values into command text, which closes the SQL-Injection vector.
    //
    // Inheritors override NewEntity and CreateModel to map a row into a
    // strongly-typed Base subclass, and supply table-specific Insert /
    // Update / Delete builders.
    public abstract class BaseDB
    {
        // Resolved on first use. Settable from the host so the DB path
        // can be made explicit (the WCF host sets it on startup).
        private static string _connectionString;
        private static readonly object _connLock = new object();

        public static string DatabaseFilePath { get; private set; }

        // Allow the host to override the default lookup. Useful when the
        // server runs from a deployed bin/ directory.
        public static void Configure(string accdbPath)
        {
            DatabaseFilePath = accdbPath;
            _connectionString =
                "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + accdbPath +
                ";Persist Security Info=False";
        }

        protected static string ConnectionString
        {
            get
            {
                lock (_connLock)
                {
                    if (_connectionString == null)
                    {
                        if (DatabaseFilePath == null)
                            DatabaseFilePath = LocateDefaultDatabase();
                        _connectionString =
                            "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" +
                            DatabaseFilePath + ";Persist Security Info=False";
                    }
                    return _connectionString;
                }
            }
        }

        // Walk up from the running executable's folder looking for a
        // ViewDB sub-directory containing the .accdb. Falls back to a
        // file dropped next to the exe.
        private static string LocateDefaultDatabase()
        {
            const string fileName = "ArchiveData3.accdb";

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 6 && dir != null; i++)
            {
                string candidate = Path.Combine(dir.FullName, "ViewDB", fileName);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            return Path.Combine(baseDir, fileName);
        }

        // Subclasses construct the right Base subtype here so the generic
        // Select can return List<Base>.
        protected abstract Base NewEntity();

        // Subclasses populate the entity's fields from the open reader.
        // Always set Id from the "ID" column.
        protected abstract Base ReadRow(OleDbDataReader reader, Base entity);

        // ----- Synchronous helpers -----

        protected int ExecuteScalarInt(string sql, params OleDbParameter[] parameters)
        {
            using (OleDbConnection conn = new OleDbConnection(ConnectionString))
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                conn.Open();
                object o = cmd.ExecuteScalar();
                if (o == null || o == DBNull.Value) return 0;
                return Convert.ToInt32(o);
            }
        }

        protected List<Base> Select(string sql, params OleDbParameter[] parameters)
        {
            List<Base> list = new List<Base>();
            using (OleDbConnection conn = new OleDbConnection(ConnectionString))
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                try
                {
                    conn.Open();
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Base entity = NewEntity();
                            entity = ReadRow(reader, entity);
                            list.Add(entity);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Select failed: " + ex.Message + "\nSQL: " + sql);
                }
            }
            return list;
        }

        // INSERT / UPDATE / DELETE. Returns affected row count.
        protected int SaveChanges(string sql, params OleDbParameter[] parameters)
        {
            using (OleDbConnection conn = new OleDbConnection(ConnectionString))
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                try
                {
                    conn.Open();
                    return cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("SaveChanges failed: " + ex.Message + "\nSQL: " + sql);
                    return 0;
                }
            }
        }

        // INSERT and return the newly generated identity (auto-number ID).
        protected int InsertAndGetId(string sql, params OleDbParameter[] parameters)
        {
            using (OleDbConnection conn = new OleDbConnection(ConnectionString))
            {
                conn.Open();
                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    try { cmd.ExecuteNonQuery(); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Insert failed: " + ex.Message + "\nSQL: " + sql);
                        return -1;
                    }
                }
                using (OleDbCommand idCmd = new OleDbCommand("SELECT @@IDENTITY", conn))
                {
                    object o = idCmd.ExecuteScalar();
                    if (o == null || o == DBNull.Value) return -1;
                    return Convert.ToInt32(o);
                }
            }
        }

        // ----- Asynchronous wrappers -----
        // OleDb itself does not implement true async I/O, so we wrap the
        // synchronous calls in Task.Run. From the caller's point of view
        // they remain non-blocking, which is what the spec asks for under
        // the "give priority to async programming" extension.

        protected Task<int> ExecuteScalarIntAsync(string sql, params OleDbParameter[] parameters)
        {
            return Task.Run(() => ExecuteScalarInt(sql, parameters));
        }

        protected Task<List<Base>> SelectAsync(string sql, params OleDbParameter[] parameters)
        {
            return Task.Run(() => Select(sql, parameters));
        }

        protected Task<int> SaveChangesAsync(string sql, params OleDbParameter[] parameters)
        {
            return Task.Run(() => SaveChanges(sql, parameters));
        }

        protected Task<int> InsertAndGetIdAsync(string sql, params OleDbParameter[] parameters)
        {
            return Task.Run(() => InsertAndGetId(sql, parameters));
        }

        // Helper so derived classes do not have to write the verbose
        // OleDbParameter constructor everywhere.
        //
        // Crucially, this maps the .NET value type to an explicit
        // OleDbType. Implicit type inference (the default
        // OleDbParameter constructor) sometimes ships C# bool values to
        // Access as a numeric, which the engine rejects with a "data
        // type mismatch in criteria expression" error. Specifying the
        // OleDbType up-front avoids that whole class of bug.
        protected static OleDbParameter P(string name, object value)
        {
            if (value == null)
                return new OleDbParameter(name, OleDbType.Variant) { Value = DBNull.Value };

            OleDbType type;
            switch (value)
            {
                case bool _:     type = OleDbType.Boolean;  break;
                case int _:      type = OleDbType.Integer;  break;
                case long _:     type = OleDbType.BigInt;   break;
                case short _:    type = OleDbType.SmallInt; break;
                case byte _:     type = OleDbType.UnsignedTinyInt; break;
                case double _:   type = OleDbType.Double;   break;
                case float _:    type = OleDbType.Single;   break;
                case decimal _:  type = OleDbType.Decimal;  break;
                case DateTime _: type = OleDbType.Date;     break;
                case Guid _:     type = OleDbType.Guid;     break;
                case string _:   type = OleDbType.VarWChar; break;
                default:         type = OleDbType.Variant;  break;
            }
            return new OleDbParameter(name, type) { Value = value };
        }

        protected static OleDbParameter P(string name, OleDbType type, object value)
        {
            OleDbParameter p = new OleDbParameter(name, type);
            p.Value = value ?? DBNull.Value;
            return p;
        }

        // Safe int reader — accepts DBNull and returns 0.
        protected static int ReadInt(OleDbDataReader r, string col)
        {
            object o = r[col];
            if (o == null || o == DBNull.Value) return 0;
            return Convert.ToInt32(o);
        }

        protected static int? ReadNullableInt(OleDbDataReader r, string col)
        {
            object o = r[col];
            if (o == null || o == DBNull.Value) return null;
            return Convert.ToInt32(o);
        }

        protected static string ReadString(OleDbDataReader r, string col)
        {
            object o = r[col];
            return o == null || o == DBNull.Value ? null : Convert.ToString(o);
        }

        protected static DateTime ReadDateTime(OleDbDataReader r, string col)
        {
            object o = r[col];
            if (o == null || o == DBNull.Value) return DateTime.MinValue;
            return Convert.ToDateTime(o);
        }

        protected static DateTime? ReadNullableDateTime(OleDbDataReader r, string col)
        {
            object o = r[col];
            if (o == null || o == DBNull.Value) return null;
            return Convert.ToDateTime(o);
        }

        protected static bool ReadBool(OleDbDataReader r, string col)
        {
            object o = r[col];
            if (o == null || o == DBNull.Value) return false;
            return Convert.ToBoolean(o);
        }
    }
}
