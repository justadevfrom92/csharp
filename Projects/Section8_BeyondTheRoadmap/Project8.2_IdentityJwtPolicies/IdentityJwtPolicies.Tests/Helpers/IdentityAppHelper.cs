using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityJwtPolicies.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IdentityJwtPolicies.Tests.Helpers;

// Each test gets its OWN app and its OWN SQLite database file, so users and lockouts never leak between tests.
public sealed class IdentityAppHelper : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"identity-test-{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<Program> factory;

    public IdentityAppHelper()
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // Pooling=False so the file is really closed and can be deleted afterwards.
            builder.UseSetting("ConnectionStrings:Identity", $"Data Source={databasePath};Pooling=False");
        });
        Client = factory.CreateClient();
    }

    public HttpClient Client { get; }

    public Task<HttpResponseMessage> RegisterAsync(string email, string password, string department = "sales", int birthYear = 1990)
    {
        return Client.PostAsJsonAsync("/account/register", new RegisterRequestModel(email, password, department, birthYear));
    }

    public Task<HttpResponseMessage> LoginAsync(string email, string password)
    {
        return Client.PostAsJsonAsync("/account/login", new LoginRequestModel(email, password));
    }

    // Register + log in, and return the JWT.
    public async Task<string> RegisterAndGetTokenAsync(string department = "sales", int birthYear = 1990)
    {
        string email = $"user{Guid.NewGuid():N}@example.com";
        (await RegisterAsync(email, "Passw0rd!", department, birthYear)).EnsureSuccessStatusCode();
        return await GetTokenAsync(email, "Passw0rd!");
    }

    public async Task<string> GetTokenAsync(string email, string password)
    {
        HttpResponseMessage response = await LoginAsync(email, password);
        response.EnsureSuccessStatusCode();
        TokenResponseModel? token = await response.Content.ReadFromJsonAsync<TokenResponseModel>();
        return token!.AccessToken;
    }

    public async Task<int> GetStatusWithTokenAsync(string path, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        HttpResponseMessage response = await Client.SendAsync(request);
        return (int)response.StatusCode;
    }

    public void Dispose()
    {
        factory.Dispose();
        File.Delete(databasePath);
    }
}
