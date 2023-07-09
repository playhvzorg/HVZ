using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace HVZ.Web.Shared
{
    public class PasswordValidationAttribute: ValidationAttribute
    {
        public int MinCharacters { get; set; }
        public int MinLowercase { get; set; }
        public int MinUppercase { get; set; }
        public int MinDigits { get; set; }
        public int MinSpecial { get; set; }

        public PasswordValidationAttribute
        (
            int minCharacters = 8,
            int minLowercase = 1,
            int minUppercase = 1,
            int minDigits = 1,
            int minSpecial = 1
        )
        {
            MinCharacters = minCharacters;
            MinLowercase = minLowercase;
            MinUppercase = minUppercase;
            MinDigits = minDigits;
            MinSpecial = minSpecial;

            if (MinLowercase + MinUppercase + MinDigits > MinCharacters)
            {
                throw new ArgumentException("Required number of characters is greater than the minimum number of caracters. Consider increasing the minimum number of characters.");
            }
        }

        private static string Plural(int count) => count == 1 ? "" : "s";

        private static string ConcatList(List<string> strings) => strings.Count == 1 ? strings[0] : 
        string.Join(",", strings.Take(strings.Count - 1)) + " and " + strings[^1];

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            string password = (string)(value ?? "");
            List<string> errors = new List<string>();
            bool failed = false;

            if (password.Length < MinCharacters)
            {
                failed = true;
                errors.Add($"at least {MinCharacters} character{Plural(MinCharacters)}");
            }

            Regex lowercasePattern = new($"(?=.*[a-z]{{{MinLowercase}}})");
            if (!lowercasePattern.IsMatch(password))
            {
                failed = true;
                errors.Add($"at least {MinLowercase} lowercase character{Plural(MinLowercase)}");
            }

            Regex uppercasePattern = new($"(?=.*[A-Z]{{{MinUppercase}}})");
            if (!uppercasePattern.IsMatch(password))
            {
                failed = true;
                errors.Add($"at least {MinUppercase} uppercase letter{Plural(MinUppercase)}");
            }

            Regex digitsPattern = new($"(?=.*\\d{{{MinDigits}}})");
            if (!digitsPattern.IsMatch(password))
            {
                failed = true;
                errors.Add($"at least {MinDigits} digit{Plural(MinDigits)}");
            }

            Regex specialPattern = new($"(?=.*[^a-zA-Z\\d]){{{MinSpecial}}}");
            if (!specialPattern.IsMatch(password))
            {
                failed = true;
                errors.Add($"at least {MinSpecial} symbol{Plural(MinSpecial)}");
            }

            if (failed)
            {
                return new ValidationResult($"Passwrod is missing the following requirements: {ConcatList(errors)}");
            }

            return ValidationResult.Success;
        }

    }

}
