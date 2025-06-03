using Microsoft.EntityFrameworkCore;
using PidgeonRenderFarm.Common.Database;
using PidgeonRenderFarm.Common.Database.Models;
using PidgeonRenderFarm.Common.Models;

namespace PidgeonRenderFarm.Node;

public class NodeContext(DbContextOptions dbContextOptions) : BaseContext<NodeContext, NodeConfiguration>(dbContextOptions)
{
    public virtual DbSet<Server> Servers { get; set; }
}