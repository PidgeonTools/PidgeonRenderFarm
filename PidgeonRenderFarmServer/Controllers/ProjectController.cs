using Microsoft.AspNetCore.Mvc;

namespace PidgeonRenderFarmServer.Controllers;

[Route("project")]
public class ProjectController : Controller
{
    [Route("new")]
    public async Task<IActionResult> New()
    {
        return View();
    }
}