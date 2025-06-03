using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using PidgeonRenderFarm.Common.Database.Models;
using PidgeonRenderFarm.Common.Enums;

namespace PidgeonRenderFarm.Common.Database.Models;

public class Project : BaseEntity
{
    public string Name { get; set; }
    public string BlendFilePath { get; set; }
    
    public long? ProjectServerID { get; set; }
    public virtual Server? ProjectServer { get; set; }
    
    public long BlenderVersionID { get; set; }
    public virtual VersionInfo? BlenderVersion { get; set; }
    
    public long RenderEngineID { get; set; }
    public virtual RenderEngine? RenderEngine { get; set; }
    
    [NotMapped]
    public bool RenderTestFrame { get; set; }
    
    public bool UseSuperFastRender { get; set; }
    public bool UseSuperImageDenoiserTemporal { get; set; }
    
    public int BatchSize { get; set; }
    public int FrameStep { get; set; }
    
    #region Video Generation
    public FrameOutputFileFormat FrameOutputFileFormat { get; set; }
    public string? FrameOutputFileExtension => FrameOutputFileFormat.ToString();
    
    public bool GenerateVideo { get; set; }
    public decimal? VideoFrameRate { get; set; }
    public VideoRateControl? VideoRateControl { get; set; }
    public int? VideoRateControlValue { get; set; }
    public VideoOutputFileFormat? VideoOutputFileFormat { get; set; }
    public string? VideoOutputFileExtension => VideoOutputFileFormat?.ToString();
    
    public bool? ResizeVideo { get; set; }
    public int? VideoResizedResolutionX { get; set; }
    public int? VideoResizedResolutionY { get; set; }
    [NotMapped]
    public Vector2? VideoResizedResolution => (VideoResizedResolutionX != null && VideoResizedResolutionY != null)
        ? new Vector2((float)VideoResizedResolutionX, (float)VideoResizedResolutionY)
        : null;
    #endregion
    
    // public long FirstFrameID { get; set; }
    // public long LastFrameID { get; set; }

    #region Calculated Values
    public decimal AverageTimePerFrame
    {
        get
        {
            return Frames?.Where(f => f.State == FrameState.Completed).Average(f => f.TimeUsed) ?? 0m;
        }
    }
    public decimal AverageRAMUsePerFrame
    {
        get
        {
            return Frames?.Where(f => f.State == FrameState.Completed).Average(f => f.RAMUsed) ?? 0m;
        }
    }
    #endregion
    
    public virtual Frame[] Frames { get; set; }
}