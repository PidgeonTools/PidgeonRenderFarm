using PidgeonRenderFarm.Common;
using PidgeonRenderFarm.Common.Models;
using PidgeonRenderFarm.Server;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages();
builder.Services.AddRazorComponents();

WebApplication app = builder.Build();
        
do
{
    ServerKernel.Start();
    
    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        // app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    
        app.Urls.Clear();
        app.Urls.Add($"https://{ServerKernel.ActiveConfiguration!.GetIPAddress()}:{ServerKernel.ActiveConfiguration!.Port}");
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    //app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    
    app.RunAsync(ServerKernel.ShutdownRequestedToken.Token).GetAwaiter().GetResult();
}
while (ServerKernel.RestartRequested);