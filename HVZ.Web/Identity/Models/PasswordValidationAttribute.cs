using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

public class PasswordValidationAttribute : ValidationAttribute {

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

        // Throw an error if required characters are more than the minimum characters
        if (MinLowercase + MinUppercase + MinDigits + MinSpecial > MinCharacters)
            throw new ArgumentException("Required number of characters is greater than minimum number of characters");
    }

    public int MinCharacters { get; set; }
    public int MinLowercase { get; set; }
    public int MinUppercase { get; set; }
    public int MinDigits { get; set; }
    public int MinSpecial { get; set; }

    private string Plural(int charCount)
    {
        return charCount == 1 ? "" : "s";
    }

    private string ConcatList(List<string> strings)
    {
        return strings.Count == 1 ? strings[0] : string.Join(", ", strings.Take(strings.Count - 1)) + " and " + strings[strings.Count - 1];
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        string password = (string)(value ?? "");
        var errors = new List<string>();
        bool failed = false;

        // Password length
        if (password.Length < MinCharacters)
        {
            failed = true;
            errors.Add($"{MinCharacters} characters");
        }

        // Number of lowercase characters
        var lowercasePattern = new Regex($"(?=.*[a-z]{{{MinLowercase}}})");
        if (!lowercasePattern.IsMatch(password))
        {
            failed = true;
            errors.Add($"{MinLowercase} lowercase character{Plural(MinLowercase)}");
        }

        // Number of uppercase characters
        var uppercasePattern = new Regex($"(?=.*[A-Z]{{{MinUppercase}}})");
        if (!uppercasePattern.IsMatch(password))
        {
            failed = true;
            errors.Add($"{MinUppercase} uppercase character{Plural(MinUppercase)}");
        }

        // Number of numberical digits
        var digitsPattern = new Regex($"(?=.*\\d{{{MinDigits}}})");
        if (!digitsPattern.IsMatch(password))
        {
            failed = true;
            errors.Add($"{MinDigits} number{Plural(MinDigits)}");
        }

        // Number of alphanumeric characters
        var specialPattern = new Regex($"(?=.*[^a-zA-Z\\d]){{{MinSpecial}}}");
        if (!specialPattern.IsMatch(password))
        {
            failed = true;
            errors.Add($"{MinSpecial} symbol{Plural(MinSpecial)}");
        }

        if (failed)
            return new ValidationResult($"Password is missing the following requirements: at least {ConcatList(errors)}");

        return ValidationResult.Success;

    }
}