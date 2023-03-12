using System.ComponentModel.DataAnnotations;
namespace HVZ.Web.Identity.Models
{
    public class ResetRequestModel
    {
        [Required]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string Email { get; set; } = "";
    }
}