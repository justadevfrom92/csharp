using CookieAuthAndRoles.Models;
using Microsoft.AspNetCore.Identity;

namespace CookieAuthAndRoles.Stores;

// In-memory users. Passwords are stored HASHED, never as plain text.
public static class UserStore
{
    private static readonly PasswordHasher<AppUserModel> passwordHasher = new();
    private static readonly List<AppUserModel> users = new();
    private static readonly object listLock = new();

    static UserStore()
    {
        Register("admin", "Admin123!", "Admin"); // demo admin account
        Register("alice", "Alice123!", "User");
    }

    public static bool Register(string userName, string password, string role)
    {
        lock (listLock)
        {
            if (users.Any(user => user.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
            var newUser = new AppUserModel { UserName = userName, Role = role };
            newUser.PasswordHash = passwordHasher.HashPassword(newUser, password);
            users.Add(newUser);
            return true;
        }
    }

    public static AppUserModel? CheckPassword(string userName, string password)
    {
        AppUserModel? user;
        lock (listLock)
        {
            user = users.FirstOrDefault(existing => existing.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
        }
        if (user == null)
        {
            return null;
        }
        PasswordVerificationResult result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }
}
