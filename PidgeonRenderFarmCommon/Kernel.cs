using Microsoft.EntityFrameworkCore;
using PidgeonRenderFarm.Common.Database;
using PidgeonRenderFarm.Common.Database.Models;
using PidgeonRenderFarm.Common.Models;

namespace PidgeonRenderFarm.Common;

public abstract class Kernel<TConfiguration, TContext>
    where TConfiguration : Configuration<TConfiguration>
    where TContext : BaseContext<TContext, TConfiguration>
{
    public static bool RestartRequested { get; private set; }
    public static CancellationTokenSource ShutdownRequestedToken { get; private set; }

    public static void RequestShutdown()
    {
        ShutdownRequestedToken.Cancel();
    }
    public static void RequestRestart()
    {
        RestartRequested = true;
        RequestShutdown();
    }
    
    public static bool RequiresConfiguration { get; protected set; } = true;
    public static TConfiguration? ActiveConfiguration { get; private set; }
    public static List<BlenderInstallation> BlenderInstallations { get; private set; }
    public static void LoadConfiguration()
    {
        LoadConfigurationAsync().GetAwaiter().GetResult();
    }
    public static async Task LoadConfigurationAsync()
    {
        await using (TContext ctx = BaseContext<TContext, TConfiguration>.GetContext())
        {
            ActiveConfiguration = await ctx.Configuration.FirstOrDefaultAsync();
            BlenderInstallations = await ctx.BlenderInstallations.ToListAsync();

            if (ActiveConfiguration != null && BlenderInstallations.Any())
            {
                RequiresConfiguration = false;
            }

            ActiveConfiguration ??= Activator.CreateInstance<TConfiguration>();
            BlenderInstallations ??= [];
        }
    }

    public static async Task<bool> SaveConfigurationAsync(TConfiguration newConfiguration)
    {
        await using (TContext ctx = BaseContext<TContext, TConfiguration>.GetContext())
        {
            // if (ActiveConfiguration == null || RequiresConfiguration)
            // {
            //     return false;
            // }

            if (await ctx.Configuration.AnyAsync())
            {
                ctx.Configuration.RemoveRange(await ctx.Configuration.ToArrayAsync());
            }

            await ctx.Configuration.AddAsync(newConfiguration);
            await ctx.SaveChangesAsync();
            await ctx.Entry(newConfiguration).ReloadAsync();
            
            ActiveConfiguration = newConfiguration;
        }
        
        return true;
    }

    public static void Start()
    {
        LoadConfiguration();
        RestartRequested = false;
        ShutdownRequestedToken = new CancellationTokenSource();
    }

    private static readonly HttpClientHandler _httpClientHandler = new HttpClientHandler()
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };
    public static readonly HttpClient HttpsClient = new HttpClient(_httpClientHandler);
}