using Microsoft.AspNetCore.Mvc;
using MyFirstApp.Models;

namespace MyFirstApp.Controllers
{
    public class CoupController : Controller
    {
        // Menyimpan status game di memory (sementara)
        static CoupGame Game = new CoupGame();

        public IActionResult Index()
        {
            return View(Game);
        }

        [HttpPost]
        public IActionResult AddPlayer(string name, string description)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                Game.AddPlayer(name, description);
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
            if (Game.GameState == "Playing")
            {
                Game.PerformAction(type, targetId);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Reset()
        {
            Game = new CoupGame();
            return RedirectToAction("Index");
        }
    }
}
