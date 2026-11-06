namespace CookieAuthAndRoles.Data.Entities;

public class AppUser
{
    public string UserName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "User";
}
