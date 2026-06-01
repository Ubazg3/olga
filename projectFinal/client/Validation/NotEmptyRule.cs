using System.Globalization;
using System.Windows.Controls;

namespace CheckersClient.Validation
{
    // Generic XAML-friendly ValidationRule. Used as a starting point for
    // the more specific UsernameRule / PasswordRule subclasses.
    public class NotEmptyRule : ValidationRule
    {
        public string FieldName { get; set; } = "Field";

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string s = value as string;
            if (string.IsNullOrWhiteSpace(s))
                return new ValidationResult(false, FieldName + " is required.");
            return ValidationResult.ValidResult;
        }
    }
}
