using System.Runtime.Serialization;

namespace Model
{
    // Reply from the server after a player attempts a move.
    [DataContract]
    public class MoveResult
    {
        [DataMember] public bool Accepted { get; set; }
        [DataMember] public string ErrorMessage { get; set; }
        [DataMember] public Move PlayedMove { get; set; }
        [DataMember] public GameStatus NewStatus { get; set; }
        [DataMember] public GameEndReason EndReason { get; set; }
        [DataMember] public string FEN { get; set; }

        public static MoveResult Fail(string error)
        {
            return new MoveResult { Accepted = false, ErrorMessage = error };
        }

        public static MoveResult Ok(Move move, GameStatus status, GameEndReason reason, string fen)
        {
            return new MoveResult
            {
                Accepted = true,
                PlayedMove = move,
                NewStatus = status,
                EndReason = reason,
                FEN = fen
            };
        }
    }
}
