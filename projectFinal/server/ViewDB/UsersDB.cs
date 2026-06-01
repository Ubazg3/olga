using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Threading.Tasks;
using Model;

namespace ViewDB
{
    // CRUD for the Users table. Inherits from BaseDB and uses
    // parameterised SQL exclusively.
    public class UsersDB : BaseDB
    {
        protected override Base NewEntity() { return new User(); }

        protected override Base ReadRow(OleDbDataReader r, Base entity)
        {
            User u = (User)entity;
            u.Id             = ReadInt(r, "ID");
            u.Username       = ReadString(r, "Username");
            u.PasswordHash   = ReadString(r, "PasswordHash");
            u.Email          = ReadString(r, "Email");
            u.Role           = (UserRole)ReadInt(r, "Role");
            u.Wins           = ReadInt(r, "Wins");
            u.Losses         = ReadInt(r, "Losses");
            u.Draws          = ReadInt(r, "Draws");
            u.Rating         = ReadInt(r, "Rating");
            u.CreatedAt      = ReadDateTime(r, "CreatedAt");
            u.IsBanned       = ReadBool(r, "IsBanned");
            u.ProfilePicture = ReadOptionalBytes(r, "ProfilePicture");
            u.BirthDate      = ReadOptionalDateTime(r, "BirthDate");
            u.Country        = ReadOptionalString  (r, "Country") ?? string.Empty;
            return u;
        }

        // Defensive readers for columns that may not exist on legacy
        // databases — same pattern as ReadOptionalBytes.
        private static DateTime? ReadOptionalDateTime(OleDbDataReader r, string column)
        {
            try
            {
                object o = r[column];
                if (o == null || o == DBNull.Value) return null;
                return Convert.ToDateTime(o);
            }
            catch (IndexOutOfRangeException) { return null; }
        }

        private static string ReadOptionalString(OleDbDataReader r, string column)
        {
            try
            {
                object o = r[column];
                return o == null || o == DBNull.Value ? null : Convert.ToString(o);
            }
            catch (IndexOutOfRangeException) { return null; }
        }

        // Reads a binary column if present, falls back to null when the
        // column is missing — keeps the layer working against legacy
        // databases that haven't gone through the ProfilePicture
        // migration yet.
        private static byte[] ReadOptionalBytes(OleDbDataReader r, string column)
        {
            try
            {
                object o = r[column];
                return o == null || o == DBNull.Value ? null : (byte[])o;
            }
            catch (IndexOutOfRangeException) { return null; }
        }

        // ----- Reads -----

        public async Task<User> GetByUsernameAsync(string username)
        {
            const string sql =
                "SELECT * FROM Users WHERE Username = ?";
            List<Base> rows = await SelectAsync(sql, P("@Username", username));
            return rows.Count == 0 ? null : (User)rows[0];
        }

        public async Task<User> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM Users WHERE ID = ?";
            List<Base> rows = await SelectAsync(sql, P("@ID", id));
            return rows.Count == 0 ? null : (User)rows[0];
        }

        public async Task<List<User>> GetAllAsync()
        {
            const string sql =
                "SELECT * FROM Users ORDER BY Rating DESC";
            List<Base> rows = await SelectAsync(sql);
            return rows.ConvertAll(b => (User)b);
        }

        public async Task<List<User>> GetTopByRatingAsync(int count)
        {
            // Access supports TOP n directly.
            string sql = "SELECT TOP " + count.ToString() +
                         " * FROM Users WHERE IsBanned = false ORDER BY Rating DESC";
            List<Base> rows = await SelectAsync(sql);
            return rows.ConvertAll(b => (User)b);
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            const string sql =
                "SELECT COUNT(*) FROM Users WHERE Username = ?";
            int count = await ExecuteScalarIntAsync(sql, P("@Username", username));
            return count > 0;
        }

        // ----- Writes -----

        public async Task<int> AddAsync(User u)
        {
            const string sql =
                "INSERT INTO Users " +
                "(Username, PasswordHash, Email, [Role], Wins, Losses, Draws, Rating, CreatedAt, IsBanned, BirthDate, Country) " +
                "VALUES (?,?,?,?,?,?,?,?,?,?,?,?)";
            // BirthDate is nullable — pass DBNull when null.
            var birthDateParam = u.BirthDate.HasValue
                ? P("@BirthDate", u.BirthDate.Value)
                : new System.Data.OleDb.OleDbParameter("@BirthDate", System.Data.OleDb.OleDbType.Date)
                  { Value = DBNull.Value };

            return await InsertAndGetIdAsync(sql,
                P("@Username",     u.Username),
                P("@PasswordHash", u.PasswordHash),
                P("@Email",        u.Email),
                P("@Role",         (int)u.Role),
                P("@Wins",         u.Wins),
                P("@Losses",       u.Losses),
                P("@Draws",        u.Draws),
                P("@Rating",       u.Rating),
                P("@CreatedAt",    u.CreatedAt),
                P("@IsBanned",     u.IsBanned),
                birthDateParam,
                P("@Country",      u.Country ?? string.Empty));
        }

        public async Task<int> UpdateStatsAsync(int userId, int wins, int losses, int draws, int rating)
        {
            const string sql =
                "UPDATE Users SET Wins = ?, Losses = ?, Draws = ?, Rating = ? WHERE ID = ?";
            return await SaveChangesAsync(sql,
                P("@Wins",   wins),
                P("@Losses", losses),
                P("@Draws",  draws),
                P("@Rating", rating),
                P("@ID",     userId));
        }

        public async Task<int> SetBannedAsync(int userId, bool banned)
        {
            const string sql = "UPDATE Users SET IsBanned = ? WHERE ID = ?";
            return await SaveChangesAsync(sql,
                P("@IsBanned", banned),
                P("@ID",       userId));
        }

        public async Task<int> UpdateEmailAsync(int userId, string email)
        {
            const string sql = "UPDATE Users SET Email = ? WHERE ID = ?";
            return await SaveChangesAsync(sql,
                P("@Email", email ?? string.Empty),
                P("@ID",    userId));
        }

        public async Task<int> UpdatePasswordHashAsync(int userId, string newHash)
        {
            const string sql = "UPDATE Users SET PasswordHash = ? WHERE ID = ?";
            return await SaveChangesAsync(sql,
                P("@PasswordHash", newHash),
                P("@ID",           userId));
        }

        // Saves (or clears with null) the avatar image bytes for a
        // single user. The column is LONGBINARY in Access — happily
        // takes byte[] up to ~64MB, far more than we'd ever send.
        public async Task<int> UpdateProfilePictureAsync(int userId, byte[] image)
        {
            const string sql = "UPDATE Users SET ProfilePicture = ? WHERE ID = ?";
            // OleDb maps byte[] to OleDbType.VarBinary; pass DBNull when
            // we want to clear the column, otherwise the parameter is
            // promoted to LONGBINARY automatically by the driver.
            var pic = image == null
                ? new System.Data.OleDb.OleDbParameter("@Pic", System.Data.OleDb.OleDbType.LongVarBinary) { Value = DBNull.Value }
                : new System.Data.OleDb.OleDbParameter("@Pic", System.Data.OleDb.OleDbType.LongVarBinary) { Value = image };
            return await SaveChangesAsync(sql, pic, P("@ID", userId));
        }

        // Single-user fetch that DOES bring back the image bytes.
        // Used only by GetUserProfile so bulk listing operations stay
        // cheap.
        public Task<byte[]> GetProfilePictureAsync(int userId)
        {
            return Task.Run(() => GetProfilePicture(userId));
        }

        private byte[] GetProfilePicture(int userId)
        {
            const string sql = "SELECT ProfilePicture FROM Users WHERE ID = ?";
            using (var conn = new System.Data.OleDb.OleDbConnection(ConnectionString))
            using (var cmd  = new System.Data.OleDb.OleDbCommand(sql, conn))
            {
                cmd.Parameters.Add(P("@ID", userId));
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    object o = reader[0];
                    return o == null || o == DBNull.Value ? null : (byte[])o;
                }
            }
        }

        public async Task<int> DeleteAsync(int userId)
        {
            const string sql = "DELETE FROM Users WHERE ID = ?";
            return await SaveChangesAsync(sql, P("@ID", userId));
        }

        // ----- Aggregates (admin dashboard) -----

        public async Task<int> CountTotalAsync()
        {
            return await ExecuteScalarIntAsync("SELECT COUNT(*) FROM Users");
        }

        public async Task<int> CountBannedAsync()
        {
            return await ExecuteScalarIntAsync("SELECT COUNT(*) FROM Users WHERE IsBanned = true");
        }

        public async Task<int> CountAdminsAsync()
        {
            return await ExecuteScalarIntAsync(
                "SELECT COUNT(*) FROM Users WHERE [Role] = ?",
                P("@Role", (int)UserRole.Admin));
        }

        public async Task<int> CountRegisteredSinceAsync(DateTime since)
        {
            return await ExecuteScalarIntAsync(
                "SELECT COUNT(*) FROM Users WHERE CreatedAt >= ?",
                P("@CreatedAt", since));
        }
    }
}
