using System.Collections.Generic;
using System.Data.OleDb;
using System.Threading.Tasks;
using Model;

namespace ViewDB
{
    // Moves is the link table for the schema: every row references a
    // Game (FK GameId), so a move is reachable from both players via
    // their Game row, satisfying the spec's "at least one link table"
    // requirement.
    public class MovesDB : BaseDB
    {
        protected override Base NewEntity() { return new Move(); }

        protected override Base ReadRow(OleDbDataReader r, Base entity)
        {
            Move m = (Move)entity;
            m.Id             = ReadInt(r, "ID");
            m.GameId         = ReadInt(r, "GameId");
            m.MoveNumber     = ReadInt(r, "MoveNumber");
            m.MoverColor     = (PieceColor)ReadInt(r, "MoverColor");
            m.FromFile       = ReadInt(r, "FromFile");
            m.FromRank       = ReadInt(r, "FromRank");
            m.ToFile         = ReadInt(r, "ToFile");
            m.ToRank         = ReadInt(r, "ToRank");
            m.PathSerialized = ReadString(r, "PathSerialized") ?? string.Empty;
            m.Piece          = (PieceType)ReadInt(r, "Piece");
            m.IsCapture      = ReadBool(r, "IsCapture");
            m.CapturedCount  = ReadInt(r, "CapturedCount");
            m.BecameKing     = ReadBool(r, "BecameKing");
            m.Notation       = ReadString(r, "Notation")   ?? string.Empty;
            m.BoardAfter     = ReadString(r, "BoardAfter") ?? string.Empty;
            m.PlayedAt       = ReadDateTime(r, "PlayedAt");
            return m;
        }

        public async Task<List<Move>> GetByGameAsync(int gameId)
        {
            const string sql =
                "SELECT * FROM Moves WHERE GameId = ? ORDER BY MoveNumber ASC, MoverColor ASC";
            List<Base> rows = await SelectAsync(sql, P("@GameId", gameId));
            return rows.ConvertAll(b => (Move)b);
        }

        public async Task<int> AddAsync(Move m)
        {
            const string sql =
                "INSERT INTO Moves " +
                "(GameId, MoveNumber, MoverColor, FromFile, FromRank, ToFile, ToRank, PathSerialized, " +
                " Piece, IsCapture, CapturedCount, BecameKing, Notation, BoardAfter, PlayedAt) " +
                "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)";
            return await InsertAndGetIdAsync(sql,
                P("@GameId",         m.GameId),
                P("@MoveNumber",     m.MoveNumber),
                P("@MoverColor",     (int)m.MoverColor),
                P("@FromFile",       m.FromFile),
                P("@FromRank",       m.FromRank),
                P("@ToFile",         m.ToFile),
                P("@ToRank",         m.ToRank),
                P("@PathSerialized", m.PathSerialized ?? string.Empty),
                P("@Piece",          (int)m.Piece),
                P("@IsCapture",      m.IsCapture),
                P("@CapturedCount",  m.CapturedCount),
                P("@BecameKing",     m.BecameKing),
                P("@Notation",       m.Notation   ?? string.Empty),
                P("@BoardAfter",     m.BoardAfter ?? string.Empty),
                P("@PlayedAt",       m.PlayedAt));
        }

        public async Task<int> DeleteByGameAsync(int gameId)
        {
            const string sql = "DELETE FROM Moves WHERE GameId = ?";
            return await SaveChangesAsync(sql, P("@GameId", gameId));
        }
    }
}
