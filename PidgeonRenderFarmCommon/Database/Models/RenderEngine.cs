namespace PidgeonRenderFarm.Common.Database.Models;

public class RenderEngine : BaseEntity
{
    public static RenderEngine Parse(string renderEngineString)
    {
        return new RenderEngine();
    }
}