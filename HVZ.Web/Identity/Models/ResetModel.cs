using System.ComponentModel.DataAnnotations;
namespace HVZ.Web.Identity.Models
{
    public class ResetModel
    {
        [Required]
        [DataType(DataType.Password)]
        [PasswordValidation]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords must match")]
        public string ConfirmPassword { get; set; } = "";
    }
}