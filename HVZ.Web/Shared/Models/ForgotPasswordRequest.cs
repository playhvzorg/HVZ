using System.ComponentModel.DataAnnotations;

namespace HVZ.Web.Shared.Models
{
    public class ForgotPasswordRequest
    {
        [EmailAddress]
        [Required]
        public string? Email { get; set; }
    }
}
