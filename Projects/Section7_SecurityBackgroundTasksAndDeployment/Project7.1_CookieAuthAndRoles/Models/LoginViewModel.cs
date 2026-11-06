using System.ComponentModel.DataAnnotations;

namespace CookieAuthAndRoles.Models;

// What the login form shows and posts.
public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your user name.")]
    [Display(Name = "User name")]
    public string UserName { get; set; } = "";

    // [DataType(DataType.Password)] makes asp-for render type="password" and never write the value back into the page.
    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    // The page the user wanted before being sent to login (from ?ReturnUrl=...). Carried in a hidden field.
    public string? ReturnUrl { get; set; }
}
