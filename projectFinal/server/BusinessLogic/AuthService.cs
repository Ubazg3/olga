using System;
using System.Threading.Tasks;
using Model;
using ViewDB;

namespace BusinessLogic
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Error  { get; set; }
        public User User     { get; set; }

        public static AuthResult Ok(User u) { return new AuthResult { Success = true,  User = u   }; }
        public static AuthResult Fail(string e) { return new AuthResult { Success = false, Error = e }; }
    }

    // Login / register layer. Holds a UsersDB and translates raw
    // credentials into User entities, hashing on the way in and verifying
    // on the way out.
    public class AuthService
    {
        private readonly UsersDB _users;

        public AuthService() { _users = new UsersDB(); }

        public async Task<AuthResult> RegisterAsync(
            string username, string password, string email,
            DateTime? birthDate = null, string country = null)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                return AuthResult.Fail("Username must be at least 3 characters.");
            if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
                return AuthResult.Fail("Password must be at least 4 characters.");

            // Light sanity-check on the optional birth date so absurd
            // values can't sneak past the client.
            if (birthDate.HasValue)
            {
                DateTime today = DateTime.Today;
                if (birthDate.Value > today)
                    return AuthResult.Fail("Birth date can't be in the future.");
                if (birthDate.Value.Year < 1900)
                    return AuthResult.Fail("Birth date is unreasonably far in the past.");
            }

            if (await _users.UsernameExistsAsync(username))
                return AuthResult.Fail("Username already taken.");

            User u = new User
            {
                Username     = username,
                PasswordHash = PasswordHasher.Hash(password),
                Email        = email ?? string.Empty,
                Role         = UserRole.Player,
                Rating       = 1200,
                CreatedAt    = DateTime.Now,
                IsBanned     = false,
                BirthDate    = birthDate,
                Country      = country ?? string.Empty
            };
            int newId = await _users.AddAsync(u);
            if (newId <= 0) return AuthResult.Fail("Could not create user.");
            u.Id = newId;
            return AuthResult.Ok(u);
        }

        public async Task<AuthResult> LoginAsync(string username, string password)
        {
            User u = await _users.GetByUsernameAsync(username);
            if (u == null)            return AuthResult.Fail("Unknown username.");
            if (u.IsBanned)           return AuthResult.Fail("Account is banned.");
            if (!PasswordHasher.Verify(password, u.PasswordHash))
                                       return AuthResult.Fail("Wrong password.");
            return AuthResult.Ok(u);
        }

        // Verifies the current password before accepting the new one.
        // Returns null on success, or an error message string.
        public async Task<string> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 4)
                return "New password must be at least 4 characters.";

            User u = await _users.GetByIdAsync(userId);
            if (u == null) return "User not found.";

            if (!PasswordHasher.Verify(currentPassword ?? string.Empty, u.PasswordHash))
                return "Current password is wrong.";

            string newHash = PasswordHasher.Hash(newPassword);
            int rows = await _users.UpdatePasswordHashAsync(userId, newHash);
            return rows > 0 ? null : "Could not save the new password.";
        }
    }
}
