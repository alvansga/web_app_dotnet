 var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
var app = builder.Build();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Game}/{action=Index}/{id?}");
    
app.Run();
