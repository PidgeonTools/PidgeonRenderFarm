using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using PidgeonRenderFarm.Common.Database.Models;
using PidgeonRenderFarm.Common.Enums;

namespace PidgeonRenderFarm.Server;

public class SmartProjectQueue : ObservableCollection<Project>
{
    private object _lock = new object();
    
    public async Task PopulateSmartQueueAsync()
    {
        Clear();
        await using (ServerContext ctx = ServerContext.GetContext())
        {
            IEnumerable<Project> projects = await ctx.Projects.ToArrayAsync();

            foreach (Project project in projects)
            {
                if (await ctx.Frames.AnyAsync(f => f.ProjectID == project.ID && f.State != FrameState.Completed))
                {
                    Enqueue(project);
                }
            }
        }
    }

    public int Enqueue(Project item)
    {
        lock (_lock)
        {
            using (ServerContext ctx = ServerContext.GetContext())
            {
                Add(item);
                ctx.Projects.AddAsync(item);
                ctx.SaveChangesAsync();
            }
        }
        
        int index = Count - 1;
        return index;
    }
    public Project? GetFirstInQueue()
    {
        Project? item = null;
        lock (_lock)
        {
            if (Count != 0)
            {
                item = this[0];
            }
        }
       
        return item;
    }
    public Project? Dequeue()
    {
        Project? item = null;
        lock (_lock)
        {
            if (Count != 0)
            {
                item = this[0];
                
                // using (ServerContext ctx = ServerContext.GetContext())
                // {
                //     ctx.Attach(item);
                //     ctx.Projects.Remove(item);
                // }
            }
        }
        
        RemoveAt(0);
        return item;
    }

    public Project? GetFirstInQueueSmart()
    {
        Project? item = null;
        lock (_lock)
        {
            using (ServerContext ctx = ServerContext.GetContext())
            {
                if (Count != 0)
                {
                    item = this.FirstOrDefault(p => ctx.Frames.Any(f => f.ProjectID == p.ID && f.State != FrameState.Completed));
                }
            }
        }
        return item;
    }
}