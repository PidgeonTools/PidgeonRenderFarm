using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PidgeonRenderFarm.Node.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}