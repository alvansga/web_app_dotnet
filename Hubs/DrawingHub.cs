using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System;

namespace WebAppSandbox.Hubs
{
    public class DrawingHub : Hub
    {
        private static readonly List<DrawData> _strokeHistory = new();
        private static readonly object _lock = new();
        private static string _currentBackground = "";

        public class DrawData
        {
            [JsonPropertyName("lastX")]
            public double LastX { get; set; }

            [JsonPropertyName("lastY")]
            public double LastY { get; set; }

            [JsonPropertyName("x")]
            public double X { get; set; }

            [JsonPropertyName("y")]
            public double Y { get; set; }

            [JsonPropertyName("color")]
            public string Color { get; set; }

            [JsonPropertyName("size")]
            public int Size { get; set; }

            [JsonPropertyName("isEraser")]
            public bool IsEraser { get; set; }
            
            [JsonPropertyName("strokeId")]
            public string StrokeId { get; set; }
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            lock (_lock)
            {
                if (_strokeHistory.Any())
                {
                    Clients.Caller.SendAsync("LoadHistory", _strokeHistory.ToArray());
                }
            }

            if (!string.IsNullOrEmpty(_currentBackground))
            {
                await Clients.Caller.SendAsync("ReceiveBackground", _currentBackground);
            }
        }

        public async Task DrawLine(DrawData drawData)
        {
            lock (_lock)
            {
                _strokeHistory.Add(drawData);
                if (_strokeHistory.Count > 50000)
                {
                    _strokeHistory.RemoveAt(0);
                }
            }

            await Clients.Others.SendAsync("ReceiveDraw", drawData);
        }

        public async Task UndoStroke()
        {
            string lastStrokeId = null;
            int removedCount = 0;

            lock (_lock)
            {
                if (!_strokeHistory.Any()) 
                {
                    Console.WriteLine("Undo requested but history is empty.");
                    return;
                }
                
                lastStrokeId = _strokeHistory.Last().StrokeId;

                if (string.IsNullOrEmpty(lastStrokeId))
                {
                    // Fallback: If for some reason we have a segment without an ID, 
                    // just remove the last segment instead of potentially wiping the clear pool
                    _strokeHistory.RemoveAt(_strokeHistory.Count - 1);
                    Console.WriteLine("Undo processed: Removed single segment because StrokeId was missing.");
                    return;
                }

                removedCount = _strokeHistory.RemoveAll(d => d.StrokeId == lastStrokeId);
                Console.WriteLine($"Undo processed: Removed {removedCount} segment(s) with StrokeId '{lastStrokeId}'.");
            }

            if (!string.IsNullOrEmpty(lastStrokeId))
            {
                await Clients.All.SendAsync("StrokeUndone", lastStrokeId);
            }
        }

        public async Task UpdateBackground(string base64Image)
        {
            _currentBackground = base64Image;
            await Clients.Others.SendAsync("ReceiveBackground", base64Image);
        }

        public async Task ClearCanvas()
        {
            lock (_lock)
            {
                _strokeHistory.Clear();
                _currentBackground = "";
            }
            await Clients.All.SendAsync("CanvasCleared");
        }
    }
}
