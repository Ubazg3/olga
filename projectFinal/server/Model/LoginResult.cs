using System;
using System.Runtime.Serialization;

namespace Model
{
    // Reply from Login / Register. Carries the session token that the
    // client must send on every subsequent call (the server is
    // stateless — there is no WCF session to fall back on).
    [DataContract]
    public class LoginResult
    {
        [DataMember] public bool   Success { get; set; }
        [DataMember] public string Error   { get; set; }
        [DataMember] public Guid   Token   { get; set; }
        [DataMember] public User   User    { get; set; }

        public static LoginResult Ok(Guid token, User user)
        {
            return new LoginResult { Success = true, Token = token, User = user };
        }

        public static LoginResult Fail(string err)
        {
            return new LoginResult { Success = false, Error = err };
        }
    }
}
