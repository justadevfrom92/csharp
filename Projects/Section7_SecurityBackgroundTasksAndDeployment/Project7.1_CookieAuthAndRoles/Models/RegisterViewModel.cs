using System.ComponentModel.DataAnnotations;

namespace CookieAuthAndRoles.Models;

// What the registration form posts. Note: there is NO Role property, so a user can't post one.
public class RegisterViewModel
{
    [Required(ErrorMessage = "Choose a user name.")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "User name must be 3 to 30 characters.")]
    [Display(Name = "User name")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}
