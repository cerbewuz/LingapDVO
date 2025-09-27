using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LingapDVO.Models;
using LingapDVO.Services;
using System.Threading.Tasks;

namespace LingapDVO.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SmsService _smsService;
    public HomeController(ILogger<HomeController> logger, SmsService smsService)
    {
        _logger = logger;
        _smsService = smsService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
