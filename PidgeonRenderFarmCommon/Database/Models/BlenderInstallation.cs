using System.Xml.Serialization;
using PidgeonRenderFarm.Common.Enums;

namespace PidgeonRenderFarm.Common.Database.Models;

public class BlenderInstallation : BaseEntity
{
    public string BinaryPath { get; set; }
    
    public long BlenderVersionID { get; set; }
    public VersionInfo? BlenderVersion { get; set; }
    
    public bool IsPTBInstalled { get; set; }
    public long? PTBVersionID { get; set; }
    public VersionInfo? PTBVersion { get; set; }
    
    public BlenderDevice Device { get; set; }
    [XmlElement(IsNullable = true)]
    public int? ThreadLimit { get; set; }
    public List<string> Allowed_Render_Engines { get; set; } // Junk code
}