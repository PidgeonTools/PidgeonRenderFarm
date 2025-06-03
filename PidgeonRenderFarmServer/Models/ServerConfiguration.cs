using Newtonsoft.Json;
using PidgeonRenderFarm.Common.Database.Models;
using PidgeonRenderFarm.Common.Models;

namespace PidgeonRenderFarm.Server.Models;

public class ServerConfiguration : Configuration<ServerConfiguration>
{
    [JsonProperty(nameof(FFmpegPath))]
    public string? FFmpegPath { get; set; }
}