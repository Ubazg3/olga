using System.Globalization;
using System.Windows.Controls;

namespace CheckersClient.Validation
{
    // XAML binding rule for the email field. Email is OPTIONAL during
    // sign-up, so empty input passes; only non-empty values are
    // validated. The actual format check lives in
    // InputValidator.ValidateEmail and is shared with the view-model
    // pre-submit guard.
    public class EmailRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string raw = value as string;
            string err = InputValidator.ValidateEmail(raw);
            return err == null
                ? ValidationResult.ValidResult
                : new ValidationResult(false, err);
        }
    }
}
