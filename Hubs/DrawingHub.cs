using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace WebAppSandbox.Hubs
{
    public class DrawingHub : Hub
    {
        private static readonly System.Collections.Concurrent.ConcurrentQueue<object> _strokeHistory = new();
        private static string _currentBackground = "";

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            // Send existing history to the newly connected client
            if (!_strokeHistory.IsEmpty)
            {
                await Clients.Caller.SendAsync("LoadHistory", _strokeHistory.ToArray());
            }

            // Send background if it exists
            if (!string.IsNullOrEmpty(_currentBackground))
            {
                await Clients.Caller.SendAsync("ReceiveBackground", _currentBackground);
            }
        }

        public async Task DrawLine(object drawData)
        {
            _strokeHistory.Enqueue(drawData);
            
            if (_strokeHistory.Count > 20000)
            {
                _strokeHistory.TryDequeue(out _);
            }

            await Clients.Others.SendAsync("ReceiveDraw", drawData);
        }

        public async Task UpdateBackground(string base64Image)
        {
            _currentBackground = base64Image;
            await Clients.Others.SendAsync("ReceiveBackground", base64Image);
        }

        public async Task ClearCanvas()
        {
            _strokeHistory.Clear();
            _currentBackground = "";
            await Clients.All.SendAsync("CanvasCleared");
        }
    }
}
