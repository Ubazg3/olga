using System.Globalization;
using System.Windows.Controls;

namespace CheckersClient.Validation
{
    // Inherits from NotEmptyRule and layers on the format check.
    // (Inheritance from a student-written class — rubric extension.)
    public class UsernameRule : NotEmptyRule
    {
        public UsernameRule() { FieldName = "Username"; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            ValidationResult baseResult = base.Validate(value, cultureInfo);
            if (!baseResult.IsValid) return baseResult;

            string err = InputValidator.ValidateUsername((string)value);
            return err == null
                ? ValidationResult.ValidResult
                : new ValidationResult(false, err);
        }
    }
}
