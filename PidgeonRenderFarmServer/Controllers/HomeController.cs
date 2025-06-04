using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PidgeonRenderFarm.Server;
using PidgeonRenderFarmServer.Models;

namespace PidgeonRenderFarmServer.Controllers;

[Route("")]
public class HomeController : Controller
{
    [Route("welcome")]
    public IActionResult Welcome()
    {
        if (!ServerKernel.RequiresConfiguration)
        {
            return Redirect("/dashboard");
        }
        return View();
    }
    
    [Route("dashboard")]
    public IActionResult Dashboard()
    {
        if (ServerKernel.RequiresConfiguration)
        {
            return Redirect("/welcome");
        }
        return View();
    }
}