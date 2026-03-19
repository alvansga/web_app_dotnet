using Microsoft.AspNetCore.Mvc;
using MyFirstApp.Models;
using Microsoft.AspNetCore.Http;

namespace MyFirstApp.Controllers
{
    public class CoupController : Controller
    {
        // Menyimpan status game di memory (sementara)
        static CoupGame Game = new CoupGame();

        public IActionResult Index()
        {
            // Ambil playerId dari session. Jika tidak ada, user belum "Join" sebagai pemain.
            ViewBag.MyPlayerId = HttpContext.Session.GetString("PlayerId");
            return View(Game);
        }

        [HttpPost]
        public IActionResult AddPlayer(string name, string description)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var newPlayer = Game.AddPlayer(name, description);
                // Simpan ID pemain ke session supaya tab ini tahu dia adalah pemain tersebut.
                HttpContext.Session.SetString("PlayerId", newPlayer.Id);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Start()
        {
            Game.StartGame();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Action(string type, string targetId)
        {
            var myId = HttpContext.Session.GetString("PlayerId");
            
            // Validasi: Apakah benar giliran si pemain yang memegang session ini?
            if (Game.GameState == "Playing" && Game.Players[Game.CurrentTurnIndex].Id == myId)
            {
                Game.PerformAction(type, targetId);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Reset()
        {
            Game = new CoupGame();
            HttpContext.Session.Clear(); // Hapus session biar semua tab join ulang
            return RedirectToAction("Index");
        }
    }
}
