namespace CheckersClient.Validation
{
    // Plain static validators used by the view-models *before* a request
    // is sent to the server. The ValidationRule subclasses in the same
    // folder wrap these for use with WPF binding validation in XAML.
    public static class InputValidator
    {
        public const int MinUsernameLength = 3;
        public const int MaxUsernameLength = 20;
        public const int MinPasswordLength = 4;

        public static string ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return "Username is required.";
            if (username.Length < MinUsernameLength)
                return $"Username must be at least {MinUsernameLength} characters.";
            if (username.Length > MaxUsernameLength)
                return $"Username must be at most {MaxUsernameLength} characters.";
            foreach (char c in username)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                    return "Username may only contain letters, digits, '_' and '-'.";
            }
            return null;
        }

        public static string ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return "Password is required.";
            if (password.Length < MinPasswordLength)
                return $"Password must be at least {MinPasswordLength} characters.";
            return null;
        }

        // Real email validation. Returns null if the email is OK or
        // empty (since email is optional during signup), otherwise a
        // human-readable explanation of what's wrong.
        //
        // Each part is checked independently so the error message
        // points at the specific problem — "Email's TLD must be at
        // least 2 characters" beats a generic "Email looks invalid".
        public static string ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;     // optional
            email = email.Trim();

            if (email.Length > 254)            return "Email is too long.";

            int at = email.IndexOf('@');
            if (at < 0)                        return "Email must contain '@'.";
            if (email.IndexOf('@', at + 1) >= 0) return "Email must not contain more than one '@'.";

            string local  = email.Substring(0, at);
            string domain = email.Substring(at + 1);

            // ---- Local part (before '@') ----
            if (local.Length == 0)               return "Email is missing the part before '@'.";
            if (local.Length > 64)               return "Email's local part is too long (max 64).";
            if (local.StartsWith(".") || local.EndsWith("."))
                                                  return "Email's local part can't start or end with a dot.";
            if (local.Contains(".."))            return "Email's local part can't contain consecutive dots.";
            foreach (char c in local)
                if (!IsLocalChar(c))             return $"Email contains an invalid character: '{c}'.";

            // ---- Domain (after '@') ----
            if (domain.Length == 0)              return "Email is missing the part after '@'.";
            if (domain.StartsWith(".") || domain.EndsWith("."))
                                                  return "Email's domain can't start or end with a dot.";
            if (domain.Contains(".."))           return "Email's domain can't contain consecutive dots.";
            foreach (char c in domain)
                if (!IsDomainChar(c))            return $"Email's domain contains an invalid character: '{c}'.";

            int lastDot = domain.LastIndexOf('.');
            if (lastDot < 0)                     return "Email's domain must contain a dot (e.g. gmail.com).";

            string tld = domain.Substring(lastDot + 1);
            if (tld.Length < 2)                  return "Email's top-level domain must be at least 2 characters.";
            foreach (char c in tld)
                if (!char.IsLetter(c))           return "Email's top-level domain must contain only letters.";

            return null;
        }

        // Allowed in the local part — RFC-style alphanumerics plus
        // common symbols: . _ % + -
        private static bool IsLocalChar(char c)
        {
            return char.IsLetterOrDigit(c)
                || c == '.' || c == '_' || c == '%' || c == '+' || c == '-';
        }

        // Allowed in domain labels — alphanumerics, dots, hyphens.
        private static bool IsDomainChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '.' || c == '-';
        }
    }
}
