// Model binding classes for login and sign up

using System.ComponentModel.DataAnnotations;

namespace HVZ.Web.Identity.Models
{
    public class AuthUserModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [Required]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string Email { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [PasswordValidation]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords must match")]
        public string ConfirmPassword { get; set; } = "";

        [Required]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the Terms of Service and the Privacy Policy")]
        public bool AgreeToTOS { get; set; }
    }

}