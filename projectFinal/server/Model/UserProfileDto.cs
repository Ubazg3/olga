using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Model
{
    // Public profile of any user. Returned by ICheckersService.GetUserProfile.
    // Holds the user's stats plus their recent games and a lookup that
    // maps participant userIds to usernames so the client can display
    // "vs alice" instead of "vs #4".
    [DataContract]
    public class UserProfileDto
    {
        [DataMember] public User              User           { get; set; }
        [DataMember] public List<GameRecord>  RecentGames    { get; set; }
        // userId  →  username, for every player who appears in
        // RecentGames. The client uses this to render the opponent
        // name next to each game row.
        [DataMember] public Dictionary<int, string> Usernames { get; set; }

        public UserProfileDto()
        {
            RecentGames = new List<GameRecord>();
            Usernames   = new Dictionary<int, string>();
        }
    }
}
