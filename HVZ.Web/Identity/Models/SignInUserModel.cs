namespace HVZ.Web.Identity.Models;

using System.ComponentModel.DataAnnotations;

public class SignInUserModel {
    [Required]
    [EmailAddress(ErrorMessage = "Invalid Email")]
    public string Email { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public bool RememberMe { get; set; }
}