using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using PidgeonRenderFarm.Common.Database.Models;
using PidgeonRenderFarm.Common.Models;

namespace PidgeonRenderFarm.Common.Database;

public abstract class BaseContext<TContext, TConfiguration>(DbContextOptions dbContextOptions) : DbContext(dbContextOptions)
    where TContext : BaseContext<TContext, TConfiguration>
    where TConfiguration : Configuration<TConfiguration>
{
    public static bool IsDatabaseInitialized { get; private set; } = false;
    public static string PathToSQLiteFile { get; private set; } = Path.Join("PidgeonRenderFarmNext.db");
    
    private static readonly PooledDbContextFactory<TContext> contextFactory = new (GetOptionBuilderWithConnection());
    public static TContext GetContext(bool lazy = true, bool autoDetectChanges = true)
    {
        if (File.Exists(PathToSQLiteFile))
        {
            File.Create(PathToSQLiteFile).Close();
        }
        TContext ctx = contextFactory.CreateDbContext();

        ctx.ChangeTracker.LazyLoadingEnabled = lazy;
        ctx.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
        
        ctx.InitializeDatabase();
        
        ActiveContexts.Add(ctx);

        return ctx;
    }
    public void InitializeDatabase()
    {
        if (IsDatabaseInitialized)
        {
            return;
        }
        
        Database.EnsureCreated();
        
        try
        {
            RelationalDatabaseCreator databaseCreator = Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
            databaseCreator.CreateTables();
        }
        catch (Exception ex) { }
        
        IsDatabaseInitialized = true;
    }
    private static DbContextOptions<TContext> GetOptionBuilderWithConnection()
    {
        DbContextOptionsBuilder<TContext> optionsBuilder = new DbContextOptionsBuilder<TContext>();
        optionsBuilder.UseSqlite($"Data Source={PathToSQLiteFile}");
        return optionsBuilder.Options;
    }
    
    public virtual DbSet<LogEntry> Logs { get; set; }
    public virtual DbSet<TConfiguration> Configuration { get; set; }
    
    public virtual DbSet<VersionInfo> BlenderVersions { get; set; }
    public virtual DbSet<VersionInfo> PTBVersions { get; set; }
    // public virtual DbSet<RenderEngine> RenderEngines { get; set; }
    public virtual DbSet<BlenderInstallation> BlenderInstallations { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlenderInstallation>()
            .HasOne(o => o.BlenderVersion)
            .WithMany()
            .HasForeignKey(fk => fk.BlenderVersionID)
            .HasPrincipalKey(fk => fk.ID)
            .OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<BlenderInstallation>()
            .HasOne(o => o.PTBVersion)
            .WithMany()
            .HasForeignKey(fk => fk.PTBVersionID)
            .HasPrincipalKey(fk => fk.ID)
            .OnDelete(DeleteBehavior.NoAction);
    }
    
    public static ObservableCollection<TContext> ActiveContexts { get; private set; } = [];

    public override void Dispose()
    {
        ActiveContexts.Remove((TContext)this);
        base.Dispose();
    }

    public override ValueTask DisposeAsync()
    {
        ActiveContexts.Remove((TContext)this);
        return base.DisposeAsync();
    }
}