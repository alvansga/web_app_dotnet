using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace WebAppSandbox.Hubs
{
    public class DrawingHub : Hub
    {
        public async Task DrawLine(object drawData)
        {
            await Clients.Others.SendAsync("ReceiveDraw", drawData);
        }

        public async Task ClearCanvas()
        {
            await Clients.All.SendAsync("CanvasCleared");
        }
    }
}
