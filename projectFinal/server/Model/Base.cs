using System.Runtime.Serialization;

namespace Model
{
    // Base entity class. Every DB-backed model inherits from it so the
    // generic BaseDB reader can populate the Id field uniformly.
    [DataContract]
    [KnownType(typeof(User))]
    [KnownType(typeof(GameRecord))]
    [KnownType(typeof(Move))]
    public class Base
    {
        [DataMember]
        public int Id { get; set; }

        public Base() { Id = -1; }
    }
}
