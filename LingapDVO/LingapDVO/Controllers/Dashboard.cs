using Microsoft.AspNetCore.Mvc;

namespace LingapDVO.Controllers
{
    public class Dashboard : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Landingpage()
        {
            return View();
        }

        public IActionResult Homepage()
        {
            return View();
        }

        public IActionResult Userprofile()
        {
            return View();
        }
    }
}
