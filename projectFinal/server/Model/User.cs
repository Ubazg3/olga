using System;
using System.Runtime.Serialization;

namespace Model
{
    // User of the chess application. Inherits the integer Id field
    // from Base so it can flow through the generic data layer.
    [DataContract]
    public class User : Base
    {
        [DataMember] public string Username { get; set; }
        [DataMember] public string PasswordHash { get; set; }
        [DataMember] public string Email { get; set; }
        [DataMember] public UserRole Role { get; set; }
        [DataMember] public int Wins { get; set; }
        [DataMember] public int Losses { get; set; }
        [DataMember] public int Draws { get; set; }
        [DataMember] public int Rating { get; set; }
        [DataMember] public DateTime CreatedAt { get; set; }
        [DataMember] public bool IsBanned { get; set; }
        // Optional avatar bytes (typically a small JPEG). Not loaded by
        // bulk-list queries to keep the wire payload small — only the
        // GetUserProfile operation includes it.
        [DataMember] public byte[] ProfilePicture { get; set; }
        // Optional profile fields collected during sign-up.
        [DataMember] public DateTime? BirthDate { get; set; }
        [DataMember] public string Country { get; set; }

        public User()
        {
            Username = string.Empty;
            PasswordHash = string.Empty;
            Email = string.Empty;
            Role = UserRole.Player;
            Rating = 1200;
            CreatedAt = DateTime.Now;
            IsBanned = false;
            ProfilePicture = null;
            BirthDate = null;
            Country = string.Empty;
        }

        // Computed age in whole years from BirthDate. null if no birthday set.
        public int? Age
        {
            get
            {
                if (!BirthDate.HasValue) return null;
                DateTime today = DateTime.Today;
                int years = today.Year - BirthDate.Value.Year;
                if (BirthDate.Value.Date > today.AddYears(-years)) years--;
                return years;
            }
        }

        public int GamesPlayed { get { return Wins + Losses + Draws; } }
    }
}
