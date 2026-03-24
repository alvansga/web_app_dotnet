using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace WebAppSandbox.Hubs
{
    public class DrawingHub : Hub
    {
        private static readonly System.Collections.Concurrent.ConcurrentQueue<object> _strokeHistory = new();

        public override async Task OnConnectedAsync()
        {
            // Send existing history to the newly connected client
            if (!_strokeHistory.IsEmpty)
            {
                await Clients.Caller.SendAsync("LoadHistory", _strokeHistory.ToArray());
            }
            await base.OnConnectedAsync();
        }

        public async Task DrawLine(object drawData)
        {
            // Store the stroke in history
            _strokeHistory.Enqueue(drawData);
            
            // Limit history to 20,000 strokes to prevent memory bloat
            if (_strokeHistory.Count > 20000)
            {
                _strokeHistory.TryDequeue(out _);
            }

            // Broadcast to others
            await Clients.Others.SendAsync("ReceiveDraw", drawData);
        }

        public async Task ClearCanvas()
        {
            _strokeHistory.Clear();
            await Clients.All.SendAsync("CanvasCleared");
        }
    }
}
