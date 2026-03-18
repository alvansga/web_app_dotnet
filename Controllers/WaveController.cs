using Microsoft.AspNetCore.Mvc;

namespace MyFirstApp.Controllers
{
    public class WaveController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
