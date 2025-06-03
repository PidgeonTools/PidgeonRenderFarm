using PidgeonRenderFarm.Common.Enums;

namespace PidgeonRenderFarm.Common.Database.Models;

public class Frame : BaseEntity
{
    public long ProjectID { get; set; }
    public virtual Project? Project { get; set; }
    
    public int Number { get; set; }
    public FrameState State { get; set; }
    
    public long? AssignedNodeID { get; set; }
    public virtual Node? AssignedNode { get; set; }
    
    public decimal? TimeUsed { get; set; }
    public decimal? RAMUsed { get; set; }
    
    public DateTime ChangeTime { get; set; }
    
    public string? FileName => Number.ToString().PadLeft(6, '0') + "." + Project.FrameOutputFileExtension;
}