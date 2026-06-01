using System.Globalization;
using System.Windows.Controls;

namespace CheckersClient.Validation
{
    // Lenient rule for the *login* form: just requires non-empty input.
    // Sign-up still uses UsernameRule / PasswordRule so brand-new
    // accounts get the strict format check.
    //
    // Inherits from NotEmptyRule with no additional behaviour — kept as
    // its own class so the rule's intent is obvious in XAML.
    public class LoginNotEmptyRule : NotEmptyRule
    {
        // Constructor lets the caller decide whether this is the
        // username field or the password field, so the error message
        // names the right one.
        public LoginNotEmptyRule() { FieldName = "Field"; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return base.Validate(value, cultureInfo);
        }
    }
}
