using System;
using System.Runtime.Serialization;

namespace Model
{
    // Board coordinate. File is 0..7 mapping to a..h, Rank is 0..7 mapping to 1..8.
    [DataContract]
    public struct Square : IEquatable<Square>
    {
        [DataMember] public int File { get; set; }
        [DataMember] public int Rank { get; set; }

        public Square(int file, int rank) { File = file; Rank = rank; }

        public bool IsOnBoard
        {
            get { return File >= 0 && File < 8 && Rank >= 0 && Rank < 8; }
        }

        public string Algebraic
        {
            get { return ((char)('a' + File)).ToString() + (Rank + 1).ToString(); }
        }

        public static Square FromAlgebraic(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length < 2)
                throw new ArgumentException("Invalid algebraic square: " + s);
            int file = char.ToLower(s[0]) - 'a';
            int rank = s[1] - '1';
            return new Square(file, rank);
        }

        public bool Equals(Square other) { return File == other.File && Rank == other.Rank; }
        public override bool Equals(object obj) { return obj is Square && Equals((Square)obj); }
        public override int GetHashCode() { return (File * 8) + Rank; }
        public static bool operator ==(Square a, Square b) { return a.Equals(b); }
        public static bool operator !=(Square a, Square b) { return !a.Equals(b); }
        public override string ToString() { return Algebraic; }
    }
}
