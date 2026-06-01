using System.Globalization;
using System.Windows.Controls;

namespace CheckersClient.Validation
{
    public class PasswordRule : NotEmptyRule
    {
        public PasswordRule() { FieldName = "Password"; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            ValidationResult baseResult = base.Validate(value, cultureInfo);
            if (!baseResult.IsValid) return baseResult;

            string err = InputValidator.ValidatePassword((string)value);
            return err == null
                ? ValidationResult.ValidResult
                : new ValidationResult(false, err);
        }
    }
}
