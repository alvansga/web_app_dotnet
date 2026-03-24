using Microsoft.AspNetCore.Mvc;

namespace WebAppSandbox.Controllers
{
    public class DrawingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
