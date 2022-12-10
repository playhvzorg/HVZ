// Model binding classes for login and sign up

using System.ComponentModel.DataAnnotations;

namespace HVZ.Web.Identity.Models
{
    public class AuthUserModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "";
        
        [Required]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string Email { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z\\d]).{8,}$", ErrorMessage = "Password does not meet minimum requirements TODO: Create custom validator")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords must match")]
        public string ConfirmPassword { get; set; } = "";

        [Required]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the Terms of Service and the Privacy Policy")]
        public bool AgreeToTOS { get; set; }
    }

    public class SignInUserModel
    {
        [Required]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string Email { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; }
    }
}