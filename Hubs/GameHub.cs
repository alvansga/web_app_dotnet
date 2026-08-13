using Microsoft.AspNetCore.SignalR;

namespace WebAppSandbox.Hubs;

public class GameHub : Hub
{
    public async Task Ping(string message)
    {
        await Clients.All.SendAsync("Pong", message, DateTimeOffset.UtcNow);
    }
}