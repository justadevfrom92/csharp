using System.Net;
using System.Net.Http.Json;
using HttpClientResilience.Models;

namespace HttpClientResilience.Tests.Fakes;

// Stands in for the REAL network. Each call to the rates service gets the next scripted answer,
// and every call is counted - so a test can prove exactly how many retries happened.
public class ScriptedRatesHandlerFake : HttpMessageHandler
{
    private readonly Queue<Func<Task<HttpResponseMessage>>> scriptedAnswers = new();
    private int callCount;

    public int CallCount
    {
        get
        {
            return Volatile.Read(ref callCount);
        }
    }

    public ScriptedRatesHandlerFake Then(HttpStatusCode status)
    {
        scriptedAnswers.Enqueue(() => Task.FromResult(new HttpResponseMessage(status)));
        return this;
    }

    public ScriptedRatesHandlerFake ThenRate(decimal rate)
    {
        scriptedAnswers.Enqueue(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new ExchangeRateModel("USD", "EUR", rate)),
        }));
        return this;
    }

    public ScriptedRatesHandlerFake ThenSlowRate(TimeSpan delay, decimal rate)
    {
        scriptedAnswers.Enqueue(async () =>
        {
            await Task.Delay(delay);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new ExchangeRateModel("USD", "EUR", rate)) };
        });
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref callCount);
        if (scriptedAnswers.Count == 0)
        {
            throw new InvalidOperationException("The test didn't script an answer for this call.");
        }
        Task<HttpResponseMessage> answer = scriptedAnswers.Dequeue()();
        // Honour cancellation (the timeout strategy cancels slow attempts).
        await Task.WhenAny(answer, Task.Delay(Timeout.Infinite, cancellationToken));
        cancellationToken.ThrowIfCancellationRequested();
        return await answer;
    }
}
