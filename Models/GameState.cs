using System;
using System.Collections.Generic;

namespace WebAppSandbox.Models
{
    public class GameState
    {
        public string RoomCode { get; set; } = "";
        public bool IsGameRunning { get; set; } = false;
        public int PlayerCount { get; set; } = 0;
        public string CurrentDrawerId { get; set; } = "";
        public string CurrentDrawerName { get; set; } = "";
        public string TargetWord { get; set; } = "";
        public DateTime GameEndTime { get; set; }
        public System.Timers.Timer? GameTimer { get; set; }
        public string CurrentBackground { get; set; } = "";
        public List<DrawData> StrokeHistory { get; set; } = new();
        public object Lock { get; } = new();
    }
}