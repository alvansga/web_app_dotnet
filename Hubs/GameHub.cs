using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MyFirstApp.Hubs
{
    public class GameHub : Hub
    {
        // Hub sederhana untuk memberitahu semua client agar refresh
        public async Task NotifyUpdate()
        {
            await Clients.All.SendAsync("ReceiveUpdate");
        }
    }
}
