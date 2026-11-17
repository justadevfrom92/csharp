using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace SignalRChat.Tests;

// Who receives what with each Clients.* target, and what a method-name typo does
// (SignalRChat/Variations/UntypedChatHubVariation.cs). Two real .NET SignalR clients: Ann sends, Bob listens.
public class VariationsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public VariationsTest(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    private async Task<(HubConnection Connection, ConcurrentQueue<string> Received)> ConnectAsync()
    {
        HubConnection connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "hubs/variations/untyped"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();
        var received = new ConcurrentQueue<string>();
        connection.On<string, string, string>("ReceiveMessage", (target, sender, text) => received.Enqueue($"{target}:{text}"));
        await connection.StartAsync();
        return (connection, received);
    }

    private static async Task SettleAsync()
    {
        await Task.Delay(1500); // long polling: give every message time to arrive (or not)
    }

    [Fact]
    public async Task Targets_PositivePositive_AllReachesEveryone_CallerOnlyTheSender()
    {
        var (ann, annReceived) = await ConnectAsync();
        var (bob, bobReceived) = await ConnectAsync();

        await ann.InvokeAsync("SendToAll", "hi all");
        await ann.InvokeAsync("SendToCaller", "just me");
        await SettleAsync();

        Assert.Contains("all:hi all", annReceived);
        Assert.Contains("all:hi all", bobReceived);
        Assert.Contains("caller:just me", annReceived);
        Assert.DoesNotContain("caller:just me", bobReceived);
        await ann.DisposeAsync();
        await bob.DisposeAsync();
    }

    [Fact]
    public async Task Targets_PositiveNegative_OthersSkipsTheSender()
    {
        var (ann, annReceived) = await ConnectAsync();
        var (bob, bobReceived) = await ConnectAsync();

        await ann.InvokeAsync("SendToOthers", "not for me");
        await SettleAsync();

        Assert.Contains("others:not for me", bobReceived);
        Assert.DoesNotContain("others:not for me", annReceived);
        await ann.DisposeAsync();
        await bob.DisposeAsync();
    }

    [Fact]
    public async Task Typo_NegativeNegative_WrongMethodName_NoErrorAndNobodyReceivesIt()
    {
        var (ann, annReceived) = await ConnectAsync();
        var (bob, bobReceived) = await ConnectAsync();

        await ann.InvokeAsync("SendWithTypo", "lost"); // completes without any error...
        await SettleAsync();

        Assert.Empty(annReceived);                     // ...and nothing arrives anywhere
        Assert.Empty(bobReceived);
        await ann.DisposeAsync();
        await bob.DisposeAsync();
    }
}
