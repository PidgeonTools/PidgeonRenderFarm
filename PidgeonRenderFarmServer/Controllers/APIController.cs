using Microsoft.AspNetCore.Mvc;
using PidgeonRenderFarm.Common.Models;

namespace PidgeonRenderFarmServer.Controllers;

[Route("api")]
public class APIController : Controller
{
    public async Task<JsonResult> GetCurrentProjectNameAsync()
    {
        return new JsonResult("");
    }
    
    [HttpGet]
    [Route("log-levels")]
    public async Task<JsonResult> GetLogLevels()
    {
        Dictionary<string, int> logLevels = Enum.GetValues(typeof(LogLevel))
            .Cast<LogLevel>()
            .ToDictionary(key => key.ToString(), value => (int)value);
        return Json(logLevels);
    }
}