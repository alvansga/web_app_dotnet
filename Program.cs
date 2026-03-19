 var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Coup}/{action=Index}/{id?}");
    
app.Run();
