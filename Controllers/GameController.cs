using Microsoft.AspNetCore.Mvc;
using MyFirstApp.Models;

namespace MyFirstApp.Controllers
{
    public class GameController : Controller
    {
        static HangmanGame game = new HangmanGame("kotlin");

        public IActionResult Index()
        {
            return View(game);
        }

        [HttpPost]
        public IActionResult Guess(char letter)
        {
            game.Guess(letter);
            return RedirectToAction("Index");
        }
    }
}