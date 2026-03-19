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
            var myId = HttpContext.Session.GetString("PlayerId");
            if (myId != null && !Game.Players.Any(p => p.Id == myId)) { HttpContext.Session.Remove("PlayerId"); myId = null; }
            ViewBag.MyPlayerId = myId;
            return View(Game);
        }

        public IActionResult GamePartial()
        {
            var myId = HttpContext.Session.GetString("PlayerId");
            if (myId != null && !Game.Players.Any(p => p.Id == myId)) { HttpContext.Session.Remove("PlayerId"); myId = null; }
            ViewBag.MyPlayerId = myId;
            return PartialView("_GameContent", Game);
        }

        private async Task NotifyClients() { await _hubContext.Clients.All.SendAsync("ReceiveUpdate"); }

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
        public async Task<IActionResult> Start() { Game.StartGame(); await NotifyClients(); return RedirectToAction("Index"); }

        [HttpPost]
        public async Task<IActionResult> Action(string type, string? targetId)
        {
            var myId = HttpContext.Session.GetString("PlayerId");
            if (Game.GameState == "Playing" && Game.Players[Game.CurrentTurnIndex].Id == myId)
            {
                Game.PerformAction(type, targetId); await NotifyClients();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> SubmitChallenge()
        {
            var myId = HttpContext.Session.GetString("PlayerId");
            if ((Game.GameState == "WaitingForChallenge" || Game.GameState == "WaitingForBlockChallenge") && myId != null)
            {
                Game.Challenge(myId); await NotifyClients();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Pass()
        {
            var myId = HttpContext.Session.GetString("PlayerId");
            if ((Game.GameState == "WaitingForChallenge" || Game.GameState == "WaitingForBlockChallenge") && myId != null)
            {
                Game.PassChallenge(myId); await NotifyClients();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Block(string role)
        {
            var myId = HttpContext.Session.GetString("PlayerId");
            if (Game.GameState == "WaitingForBlock" && myId != null)
            {
                Game.Block(myId, role); await NotifyClients();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> PassBlock()
        {
            var myId = HttpContext.Session.GetString("PlayerId");
            if (Game.GameState == "WaitingForBlock" && myId != null)
            {
                Game.PassBlock(myId); await NotifyClients();
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
