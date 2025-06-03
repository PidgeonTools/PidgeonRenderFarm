using Microsoft.EntityFrameworkCore;
using PidgeonRenderFarm.Common.Database;
using PidgeonRenderFarm.Common.Database.Models;
using PidgeonRenderFarm.Server.Models;

namespace PidgeonRenderFarm.Server;

public class ServerContext(DbContextOptions dbContextOptions) : BaseContext<ServerContext, ServerConfiguration>(dbContextOptions)
{
    public virtual DbSet<Node> Nodes { get; set; }
    
    public virtual DbSet<Project> Projects { get; set; }
    public virtual DbSet<Frame> Frames { get; set; }
}