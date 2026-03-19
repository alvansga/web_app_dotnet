using Microsoft.AspNetCore.Mvc;
using MyFirstApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using MyFirstApp.Hubs;
using System.Threading.Tasks;

namespace MyFirstApp.Controllers
{
    public class CoupController : Controller
    {
        // Menyimpan status game di memory (sementara)
        static CoupGame Game = new CoupGame();
        private readonly IHubContext<GameHub> _hubContext;

        public CoupController(IHubContext<GameHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            ViewBag.MyPlayerId = HttpContext.Session.GetString("PlayerId");
            return View(Game);
        }

        public IActionResult GamePartial()
        {
            ViewBag.MyPlayerId = HttpContext.Session.GetString("PlayerId");
            return PartialView("_GameContent", Game);
        }

        private async Task NotifyClients()
        {
            await _hubContext.Clients.All.SendAsync("ReceiveUpdate");
        }

        [HttpPost]
        public async Task<IActionResult> AddPlayer(string name, string description)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var newPlayer = Game.AddPlayer(name, description);
                HttpContext.Session.SetString("PlayerId", newPlayer.Id);
                await NotifyClients();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Start()
        {
            Game.StartGame();
            await NotifyClients();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Action(string type, string? targetId)
        {
            var myId = HttpContext.Session.GetString("PlayerId");
            if (Game.GameState == "Playing" && Game.Players[Game.CurrentTurnIndex].Id == myId)
            {
                Game.PerformAction(type, targetId);
                await NotifyClients();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Challenge()
        {
            var myId = HttpContext.Session.GetString("PlayerId");
            if (Game.GameState == "WaitingForChallenge" && myId != null && myId != Game.PendingAction?.SourceId)
            {
                Game.Challenge(myId);
                await NotifyClients();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Pass()
        {
            var myId = HttpContext.Session.GetString("PlayerId");
            if (Game.GameState == "WaitingForChallenge" && myId != null && myId != Game.PendingAction?.SourceId)
            {
                Game.PassChallenge(myId);
                await NotifyClients();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Reset()
        {
            Game = new CoupGame();
            HttpContext.Session.Clear();
            await NotifyClients();
            return RedirectToAction("Index");
        }
    }
}
