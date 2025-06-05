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
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>()
            .HasOne(p => p.BlenderVersion)
            .WithMany()
            .HasForeignKey(p => p.BlenderVersionID)
            .HasPrincipalKey(bv => bv.ID)
            .OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Project>()
            .HasOne(p => p.BlenderVersion)
            .WithMany()
            .HasForeignKey(p => p.BlenderVersionID)
            .HasPrincipalKey(bv => bv.ID)
            .OnDelete(DeleteBehavior.NoAction);
        
        modelBuilder.Entity<Frame>()
            .HasOne(f => f.Project)
            .WithMany(p => p.Frames)
            .HasForeignKey(f => f.ProjectID)
            .HasPrincipalKey(p => p.ID)
            .OnDelete(DeleteBehavior.NoAction);
    }
}