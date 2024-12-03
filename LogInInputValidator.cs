using System.Linq;
using System.Text.RegularExpressions;

namespace DMZ.Legacy.LoginScreen
{
    public class LogInInputValidator
    {
        private const int MinName = 3;
        private const int MaxName = 20;
        private const int MinPassword = 8;
        private const int MaxPassword = 30;

        private static readonly Regex NameRegex = new("^[a-zA-Z0-9._@-]*$", RegexOptions.Compiled);


        public NameValidationType ValidateLogInInputName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return NameValidationType.MinLength;
            }

            NameValidationType validationType = 0;

            if (value.Length < 1)
                validationType |= NameValidationType.MinLength;

            return validationType;
        }

        public PasswordValidationType ValidateLogInInputPassword(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return PasswordValidationType.MinLength;
            }

            PasswordValidationType validationType = 0;

            if (value.Length < 1)
                validationType |= PasswordValidationType.MinLength;

            return validationType;
        }

        public NameValidationType ValidateSignUpInputName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return NameValidationType.MinLength;
            }

            NameValidationType validationType = 0;

            if (value.Length < MinName)
                validationType |= NameValidationType.MinLength;
            if (value.Length > MaxName)
                validationType |= NameValidationType.MaxLength;
            if (!NameRegex.IsMatch(value))
                validationType |= NameValidationType.InvalidCharacters;

            return validationType;
        }

        public PasswordValidationType ValidateSignUpInputPassword(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return PasswordValidationType.UpperCase |
                       PasswordValidationType.LowerCase |
                       PasswordValidationType.Digits |
                       PasswordValidationType.Symbols |
                       PasswordValidationType.MinLength;
            }

            PasswordValidationType validationType = 0;

            if (!HasUpperCase(value))
                validationType |= PasswordValidationType.UpperCase;
            if (!HasLowerCase(value))
                validationType |= PasswordValidationType.LowerCase;
            if (!HasDigit(value))
                validationType |= PasswordValidationType.Digits;
            if (!HasSymbol(value))
                validationType |= PasswordValidationType.Symbols;
            if (value.Length < MinPassword)
                validationType |= PasswordValidationType.MinLength;
            if (value.Length > MaxPassword)
                validationType |= PasswordValidationType.MaxLength;

            return validationType;
        }

        private bool HasUpperCase(string value)
        {
            return !string.IsNullOrEmpty(value) && value.Any(char.IsUpper);
        }

        private bool HasLowerCase(string value)
        {
            return !string.IsNullOrEmpty(value) && value.Any(char.IsLower);
        }

        private bool HasDigit(string value)
        {
            return !string.IsNullOrEmpty(value) && value.Any(char.IsDigit);
        }

        private bool HasSymbol(string value)
        {
            return !string.IsNullOrEmpty(value) && value.Any(ch => !char.IsLetterOrDigit(ch));
        }
    }
}