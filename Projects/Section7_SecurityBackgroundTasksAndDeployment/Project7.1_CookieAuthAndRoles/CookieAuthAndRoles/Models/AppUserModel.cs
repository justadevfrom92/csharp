namespace CookieAuthAndRoles.Models;

public class AppUserModel
{
    public string UserName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "User";
}
