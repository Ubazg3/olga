using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Threading.Tasks;
using Model;

namespace ViewDB
{
    public class GamesDB : BaseDB
    {
        protected override Base NewEntity() { return new GameRecord(); }

        protected override Base ReadRow(OleDbDataReader r, Base entity)
        {
            GameRecord g = (GameRecord)entity;
            g.Id            = ReadInt(r, "ID");
            g.WhitePlayerId = ReadInt(r, "WhitePlayerId");
            g.BlackPlayerId = ReadInt(r, "BlackPlayerId");
            g.StartedAt     = ReadDateTime(r, "StartedAt");
            g.EndedAt       = ReadNullableDateTime(r, "EndedAt");
            g.Status        = (GameStatus)ReadInt(r, "Status");
            g.EndReason     = (GameEndReason)ReadInt(r, "EndReason");
            g.WinnerId      = ReadNullableInt(r, "WinnerId");
            g.FinalBoard    = ReadString(r, "FinalBoard") ?? string.Empty;
            g.MoveCount     = ReadInt(r, "MoveCount");
            return g;
        }

        // ----- Reads -----

        public async Task<GameRecord> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM Games WHERE ID = ?";
            List<Base> rows = await SelectAsync(sql, P("@ID", id));
            return rows.Count == 0 ? null : (GameRecord)rows[0];
        }

        public async Task<List<GameRecord>> GetByPlayerAsync(int userId)
        {
            const string sql =
                "SELECT * FROM Games " +
                "WHERE WhitePlayerId = ? OR BlackPlayerId = ? " +
                "ORDER BY StartedAt DESC";
            List<Base> rows = await SelectAsync(sql,
                P("@WhitePlayerId", userId),
                P("@BlackPlayerId", userId));
            return rows.ConvertAll(b => (GameRecord)b);
        }

        public async Task<List<GameRecord>> GetRecentAsync(int count)
        {
            string sql = "SELECT TOP " + count.ToString() +
                         " * FROM Games ORDER BY StartedAt DESC";
            List<Base> rows = await SelectAsync(sql);
            return rows.ConvertAll(b => (GameRecord)b);
        }

        // ----- Writes -----

        public async Task<int> AddAsync(GameRecord g)
        {
            const string sql =
                "INSERT INTO Games " +
                "(WhitePlayerId, BlackPlayerId, StartedAt, EndedAt, Status, EndReason, WinnerId, FinalBoard, MoveCount) " +
                "VALUES (?,?,?,?,?,?,?,?,?)";
            return await InsertAndGetIdAsync(sql,
                P("@WhitePlayerId", g.WhitePlayerId),
                P("@BlackPlayerId", g.BlackPlayerId),
                P("@StartedAt",     g.StartedAt),
                P("@EndedAt",       (object)g.EndedAt ?? DBNull.Value),
                P("@Status",        (int)g.Status),
                P("@EndReason",     (int)g.EndReason),
                P("@WinnerId",      (object)g.WinnerId ?? DBNull.Value),
                P("@FinalBoard",    g.FinalBoard ?? string.Empty),
                P("@MoveCount",     g.MoveCount));
        }

        public async Task<int> UpdateOnEndAsync(int gameId, GameStatus status, GameEndReason reason,
                                                int? winnerId, string finalBoard, int moveCount, DateTime endedAt)
        {
            const string sql =
                "UPDATE Games SET Status = ?, EndReason = ?, WinnerId = ?, " +
                "FinalBoard = ?, MoveCount = ?, EndedAt = ? WHERE ID = ?";
            return await SaveChangesAsync(sql,
                P("@Status",     (int)status),
                P("@EndReason",  (int)reason),
                P("@WinnerId",   (object)winnerId ?? DBNull.Value),
                P("@FinalBoard", finalBoard ?? string.Empty),
                P("@MoveCount",  moveCount),
                P("@EndedAt",    endedAt),
                P("@ID",         gameId));
        }

        public async Task<int> DeleteAsync(int gameId)
        {
            const string sql = "DELETE FROM Games WHERE ID = ?";
            return await SaveChangesAsync(sql, P("@ID", gameId));
        }

        // ----- Aggregates (admin dashboard) -----

        public async Task<int> CountTotalAsync()
        {
            return await ExecuteScalarIntAsync("SELECT COUNT(*) FROM Games");
        }

        public async Task<int> CountSinceAsync(DateTime since)
        {
            return await ExecuteScalarIntAsync(
                "SELECT COUNT(*) FROM Games WHERE StartedAt >= ?",
                P("@StartedAt", since));
        }
    }
}
