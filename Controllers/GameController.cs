using Microsoft.AspNetCore.Mvc;
using WebAppSandbox.Models;
using System;
using System.Collections.Generic;

namespace WebAppSandbox.Controllers
{
    public class GameController : Controller
    {
        static List<string> WordList = new List<string> { "kotlin", "javascript", "csharp", "python", "golang" };
        static Random Rnd = new Random();
        static HangmanGame game = new HangmanGame(WordList[Rnd.Next(WordList.Count)]);

        public IActionResult Index()
        {
            return View(game);
        }

        [HttpPost]
        public IActionResult Guess(char letter)
        {
            if (char.IsLetter(letter))
            {
                game.Guess(char.ToLower(letter));
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Reset()
        {
            game = new HangmanGame(WordList[Rnd.Next(WordList.Count)]);
            return RedirectToAction("Index");
        }
    }
}