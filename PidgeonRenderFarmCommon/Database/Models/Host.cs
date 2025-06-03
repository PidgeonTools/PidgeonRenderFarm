namespace PidgeonRenderFarm.Common.Database.Models;

public class Host : BaseEntity
{
    public string IP { get; set; }
    public string Hostname { get; set; }
    
    public string DisplayName { get; set; }
}