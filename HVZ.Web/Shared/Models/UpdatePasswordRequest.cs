using System.ComponentModel.DataAnnotations;

// Can be reused in both changing manually and updating with email
namespace HVZ.Web.Shared.Models
{
    public class UpdatePasswordRequest
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [PasswordValidation]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "Passwords must match")]
        public string ConfirmPassword { get; set; }
    }

}
