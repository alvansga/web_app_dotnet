 var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSignalR(); // Tambah SignalR

var app = builder.Build();

app.UseSession();
app.MapHub<MyFirstApp.Hubs.GameHub>("/gameHub"); // Map SignalR Hub

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Coup}/{action=Index}/{id?}");
    
app.Run();
