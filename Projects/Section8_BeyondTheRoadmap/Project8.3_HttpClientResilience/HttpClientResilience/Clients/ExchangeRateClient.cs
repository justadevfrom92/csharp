using System.Net;
using System.Net.Http.Json;
using HttpClientResilience.Models;

namespace HttpClientResilience.Clients;

// A TYPED CLIENT: a normal class that receives a ready-made HttpClient in its constructor.
// IHttpClientFactory creates that HttpClient with the base address, timeout and resilience pipeline from
// Program.cs - this class only knows "how to ask for a rate".
public class ExchangeRateClient
{
    private readonly HttpClient httpClient;

    public ExchangeRateClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    // Returns null when the rates service says the pair doesn't exist (404).
    // Throws HttpRequestException when the service keeps failing even after the retries.
    public async Task<ExchangeRateModel?> GetRateAsync(string from, string to, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync($"rates/{from}/{to}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode(); // any other error status -> HttpRequestException
        return await response.Content.ReadFromJsonAsync<ExchangeRateModel>(cancellationToken);
    }
}
