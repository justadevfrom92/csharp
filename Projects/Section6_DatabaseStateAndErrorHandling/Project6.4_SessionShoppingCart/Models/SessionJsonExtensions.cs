using System.Text.Json;

namespace SessionShoppingCart.Models;

// Session can only store strings and bytes, so objects are saved as JSON text.
public static class SessionJsonExtensions
{
    public static void SetObjectAsJson<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T? GetObjectFromJson<T>(this ISession session, string key)
    {
        string? json = session.GetString(key);
        if (json == null)
        {
            return default;
        }
        return JsonSerializer.Deserialize<T>(json);
    }
}
