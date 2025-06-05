using Microsoft.AspNetCore.Mvc;
using PidgeonRenderFarm.Common.Enums;
using PidgeonRenderFarm.Common.Models;
using PidgeonRenderFarm.Server;

namespace PidgeonRenderFarmServer.Controllers;

[Route("api")]
public class APIController : Controller
{
    
    [HttpGet]
    [Route("log-levels")]
    public async Task<JsonResult> GetLogLevels()
    {
        Dictionary<string, int> logLevels = Enum.GetValues(typeof(LogLevel))
            .Cast<LogLevel>()
            .ToDictionary(key => key.ToString(), value => (int)value);
        return Json(logLevels);
    }
    
    [HttpGet]
    [Route("blender-devices")]
    public async Task<JsonResult> GetBlenderDevices()
    {
        Dictionary<string, int> blenderDevices = Enum.GetValues(typeof(BlenderDevice))
            .Cast<BlenderDevice>()
            .ToDictionary(key => key.ToString(), value => (int)value);
        return Json(blenderDevices);
    }
    
    [HttpGet]
    [Route("shutdown")]
    public void ShutdownApplication()
    {
        ServerKernel.RequestShutdown();
    }
    [HttpGet]
    [Route("restart")]
    public void RestartApplication()
    {
        ServerKernel.RequestRestart();
    }
}