using System;
using System.Security.Cryptography;
using System.Text;

namespace BusinessLogic
{
    // PBKDF2-SHA256 password hashing with a per-user random salt. Hashes
    // are stored as "iterations.base64salt.base64hash" so a verification
    // attempt is self-contained and the iteration count can be raised
    // without breaking old rows.
    public static class PasswordHasher
    {
        private const int SaltBytes  = 16;
        private const int HashBytes  = 32;
        private const int Iterations = 60_000;

        public static string Hash(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            byte[] salt = new byte[SaltBytes];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            byte[] hash = Pbkdf2(password, salt, Iterations, HashBytes);

            return Iterations.ToString() + "." +
                   Convert.ToBase64String(salt)  + "." +
                   Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;
            string[] parts = stored.Split('.');
            if (parts.Length != 3) return false;

            int iter;
            if (!int.TryParse(parts[0], out iter)) return false;

            byte[] salt, expected;
            try
            {
                salt     = Convert.FromBase64String(parts[1]);
                expected = Convert.FromBase64String(parts[2]);
            }
            catch { return false; }

            byte[] actual = Pbkdf2(password, salt, iter, expected.Length);
            return ConstantTimeEquals(actual, expected);
        }

        private static byte[] Pbkdf2(string password, byte[] salt, int iterations, int outputBytes)
        {
            using (var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                return kdf.GetBytes(outputBytes);
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
