using Microsoft.AspNetCore.Mvc;
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