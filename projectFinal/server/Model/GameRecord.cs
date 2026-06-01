using System;
using System.Runtime.Serialization;

namespace Model
{
    // Persistent record of a played game. The Moves table links to this
    // record via GameId, making Moves the link table required by the
    // ministry spec (Game ↔ Moves ↔ User).
    [DataContract]
    public class GameRecord : Base
    {
        [DataMember] public int WhitePlayerId { get; set; }
        [DataMember] public int BlackPlayerId { get; set; }
        [DataMember] public DateTime StartedAt { get; set; }
        [DataMember] public DateTime? EndedAt { get; set; }
        [DataMember] public GameStatus Status { get; set; }
        [DataMember] public GameEndReason EndReason { get; set; }
        [DataMember] public int? WinnerId { get; set; }
        [DataMember] public string FinalBoard { get; set; }
        [DataMember] public int MoveCount { get; set; }

        public GameRecord()
        {
            StartedAt = DateTime.Now;
            Status = GameStatus.InProgress;
            EndReason = GameEndReason.None;
            FinalBoard = string.Empty;
        }
    }
}
