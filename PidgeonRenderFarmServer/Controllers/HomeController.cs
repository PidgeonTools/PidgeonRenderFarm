using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PidgeonRenderFarmServer.Models;

namespace PidgeonRenderFarmServer.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        
        return View();
    }
}