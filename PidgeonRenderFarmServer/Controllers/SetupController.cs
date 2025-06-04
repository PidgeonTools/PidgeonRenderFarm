using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PidgeonRenderFarm.Common;
using PidgeonRenderFarm.Server;
using PidgeonRenderFarm.Server.Models;

namespace PidgeonRenderFarmServer.Controllers;

[Route("setup")]
public class SetupController : Controller
{
    [Route("configuration")]
    public async Task<IActionResult> Configuration()
    {
        return View();
    }
    
    [HttpGet]
    [Route("api/configuration")]
    public async Task<JsonResult> GetCurrentConfiguration()
    {
        return Json(ServerKernel.ActiveConfiguration);
    }
    [HttpPost]
    [Route("api/configuration")]
    public async Task<JsonResult> SetCurrentConfiguration(ServerConfiguration receivedConfiguration)
    {
        
        //ServerKernel.SaveConfigurationAsync(receivedConfiguration);
        
        return Json(true);
    }
    
    [HttpGet]
    [Route("api/blender-installations")]
    public async Task<JsonResult> GetCurrentBlenderInstallations()
    {
        return Json(ServerKernel.BlenderInstallations);
    }
    [HttpPost]
    [Route("api/blender-installations")]
    public async Task<bool> SetCurrentBlenderInstallations()
    {
        return true;
    }
}