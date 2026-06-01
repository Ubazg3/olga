using System.Runtime.Serialization;

namespace Model
{
    // Aggregate counters returned by ICheckersService.GetServerStats.
    // Powers the admin web dashboard.
    [DataContract]
    public class ServerStatsDto
    {
        [DataMember] public int TotalUsers      { get; set; }
        [DataMember] public int TotalGames      { get; set; }
        [DataMember] public int OnlineUsers     { get; set; }
        [DataMember] public int ActiveGames     { get; set; }
        [DataMember] public int BannedUsers     { get; set; }
        [DataMember] public int AdminUsers      { get; set; }
        [DataMember] public int GamesToday      { get; set; }
        [DataMember] public int RegisteredToday { get; set; }
    }
}
