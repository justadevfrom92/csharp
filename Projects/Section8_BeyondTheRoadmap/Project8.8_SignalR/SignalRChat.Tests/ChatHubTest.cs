using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using SignalRChat.Constants;
using SignalRChat.Tests.Helpers;

namespace SignalRChat.Tests;

// PositivePositive - valid input -> succeeds and the right people receive it
// PositiveNegative - well-formed input a rule refuses -> HubException / not delivered
// NegativeNegative - malformed or missing input -> refused
public class ChatHubTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ChatHubTest(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    private static string NewRoom()
    {
        return "room-" + Guid.NewGuid().ToString("N").Substring(0, 8); // unique, so tests never share a room
    }

    // ---------- Connect ----------

    [Fact]
    public async Task Connect_PositivePositive_WithUserName_IsConnected()
    {
        await using ChatClientHelper alice = await ChatClientHelper.ConnectAsync(factory, "Alice");

        Assert.Equal(HubConnectionState.Connected, alice.Connection.State);
    }

    [Fact]
    public async Task Connect_PositiveNegative_UserNameTooLong_IsRefusedBeforeConnecting()
    {
        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(() => ChatClientHelper.ConnectAsync(factory, new string('a', 31)));

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, error.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Connect_NegativeNegative_MissingUserName_IsRefusedBeforeConnecting(string? userName)
    {
        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(() => ChatClientHelper.ConnectAsync(factory, userName));

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, error.StatusCode);
    }

    // ---------- JoinRoom ----------

    [Fact]
    public async Task JoinRoom_PositivePositive_ValidRoom_EveryoneInRoomSeesTheJoin()
    {
        string room = NewRoom();
        await using ChatClientHelper alice = await ChatClientHelper.ConnectAsync(factory, "Alice");
        await alice.Connection.InvokeAsync("JoinRoom", room);
        await using ChatClientHelper bob = await ChatClientHelper.ConnectAsync(factory, "Bob");

        await bob.Connection.InvokeAsync("JoinRoom", room);

        Assert.Equal($"[{room}] Bob joined", await alice.WaitForAsync("Bob joined"));
    }

    [Fact]
    public async Task JoinRoom_PositiveNegative_UpperCaseRoomName_IsRefused()
    {
        await using ChatClientHelper alice = await ChatClientHelper.ConnectAsync(factory, "Alice");

        HubException error = await Assert.ThrowsAsync<HubException>(() => alice.Connection.InvokeAsync("JoinRoom", "General"));
        Assert.Contains("lower-case", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("way-too-long-room-name-here")]
    public async Task JoinRoom_NegativeNegative_MalformedRoomName_IsRefused(string room)
    {
        await using ChatClientHelper alice = await ChatClientHelper.ConnectAsync(factory, "Alice");

        await Assert.ThrowsAsync<HubException>(() => alice.Connection.InvokeAsync("JoinRoom", room));
    }

    // ---------- SendMessage ----------

    [Fact]
    public async Task SendMessage_PositivePositive_ReachesRoomMembersOnly()
    {
        string room = NewRoom();
        await using ChatClientHelper alice = await ChatClientHelper.ConnectAsync(factory, "Alice");
        await using ChatClientHelper bob = await ChatClientHelper.ConnectAsync(factory, "Bob");
        await using ChatClientHelper carol = await ChatClientHelper.ConnectAsync(factory, "Carol");
        await alice.Connection.InvokeAsync("JoinRoom", room);
        await bob.Connection.InvokeAsync("JoinRoom", room);
        await carol.Connection.InvokeAsync("JoinRoom", NewRoom()); // a different room

        await alice.Connection.InvokeAsync("SendMessage", room, "  Hi Bob!  ");

        Assert.Equal($"[{room}] Alice: Hi Bob!", await bob.WaitForAsync("Hi Bob!"));
        await Task.Delay(500);
        Assert.DoesNotContain(carol.Received, line => line.Contains("Hi Bob!"));
    }

    [Fact]
    public async Task SendMessage_PositiveNegative_RoomNotJoinedOrTooLong_IsRefused()
    {
        string room = NewRoom();
        await using ChatClientHelper alice = await ChatClientHelper.ConnectAsync(factory, "Alice");
        await alice.Connection.InvokeAsync("JoinRoom", room);

        HubException notJoined = await Assert.ThrowsAsync<HubException>(() => alice.Connection.InvokeAsync("SendMessage", "some-other-room", "hello"));
        HubException tooLong = await Assert.ThrowsAsync<HubException>(() => alice.Connection.InvokeAsync("SendMessage", room, new string('x', ChatRuleConstants.MaximumMessageLength + 1)));

        Assert.Contains("Join the room", notJoined.Message);
        Assert.Contains("at most 200", tooLong.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public async Task SendMessage_NegativeNegative_EmptyText_IsRefused(string text)
    {
        string room = NewRoom();
        await using ChatClientHelper alice = await ChatClientHelper.ConnectAsync(factory, "Alice");
        await alice.Connection.InvokeAsync("JoinRoom", room);

        HubException error = await Assert.ThrowsAsync<HubException>(() => alice.Connection.InvokeAsync("SendMessage", room, text));
        Assert.Contains("Empty messages", error.Message);
    }

    // ---------- Announce (IHubContext, from a normal HTTP endpoint) ----------

    [Fact]
    public async Task Announce_PositivePositive_ReachesEveryConnectedClient()
    {
        await using ChatClientHelper alice = await ChatClientHelper.ConnectAsync(factory, "Alice");
        await using ChatClientHelper bob = await ChatClientHelper.ConnectAsync(factory, "Bob");
        string text = "Restart at " + Guid.NewGuid().ToString("N").Substring(0, 6);

        HttpResponseMessage response = await factory.CreateClient().PostAsync("/announce?text=" + Uri.EscapeDataString(text), null);

        Assert.Equal(202, (int)response.StatusCode);
        await alice.WaitForAsync(text);
        await bob.WaitForAsync(text);
    }

    [Fact]
    public async Task Announce_PositiveNegative_ClientNotInAnyRoom_StillReceivesIt()
    {
        // Announcements go to ALL connections, not to rooms - a client that joined nothing still gets them.
        await using ChatClientHelper loner = await ChatClientHelper.ConnectAsync(factory, "Loner");
        string text = "For everyone " + Guid.NewGuid().ToString("N").Substring(0, 6);

        await factory.CreateClient().PostAsync("/announce?text=" + Uri.EscapeDataString(text), null);

        await loner.WaitForAsync(text);
    }

    [Fact]
    public async Task Announce_NegativeNegative_MissingText_Returns400AndSendsNothing()
    {
        await using ChatClientHelper alice = await ChatClientHelper.ConnectAsync(factory, "Alice");

        HttpResponseMessage response = await factory.CreateClient().PostAsync("/announce", null);

        Assert.Equal(400, (int)response.StatusCode);
        await Task.Delay(500);
        Assert.DoesNotContain(alice.Received, line => line.StartsWith("ANNOUNCEMENT"));
    }
}
