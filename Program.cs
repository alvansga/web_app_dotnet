 var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(hubOptions => {
    hubOptions.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
});

var app = builder.Build();
app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Drawing}/{action=Index}/{id?}");

app.MapHub<WebAppSandbox.Hubs.DrawingHub>("/drawingHub");

app.Run();
