using System;

namespace WebAppSandbox.Models
{
    public class GameState
    {
        public bool IsGameRunning { get; set; } = false;
        public string CurrentDrawerId { get; set; } = "";
        public string CurrentDrawerName { get; set; } = "";
        public string TargetWord { get; set; } = "";
        public DateTime GameEndTime { get; set; }
        public System.Timers.Timer? GameTimer { get; set; }
        public string CurrentBackground { get; set; } = "";
    }
}