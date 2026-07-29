using CodenameApp.Application.Interfaces;
using CodenameApp.Application.Services;
using CodenameApp.Infrastructure.Data;
using CodenameApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "codename.db");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// ── CORS: hanya allow origin spesifik ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("GameCorsPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5000",
                "http://localhost:5001",
                "https://localhost:5001",
                "http://127.0.0.1:5000"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ── Rate Limiting: 100 request/menit per IP ──
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("fixed", config =>
    {
        config.PermitLimit = 100;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 10;
    });
});

// ── AntiForgery untuk CSRF protection ──
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "CSRF-TOKEN";
    options.Cookie.HttpOnly = false;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddScoped<ICodenameRepository, CodenameRepository>();
builder.Services.AddScoped<CreateCodenameService>();
builder.Services.AddScoped<GetCodenameService>();
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<CreatePlayerService>();
builder.Services.AddScoped<CreateRoomService>();
builder.Services.AddScoped<JoinRoomService>();
builder.Services.AddScoped<IGameRoomRepository, GameRoomRepository>();
builder.Services.AddScoped<GetRoomDetailService>();
builder.Services.AddScoped<StartGameService>();
builder.Services.AddScoped<GetAllPlayersService>();
builder.Services.AddScoped<AssignRoleService>();
builder.Services.AddScoped<RevealCardService>();
builder.Services.AddScoped<DeleteRoomService>();
builder.Services.AddScoped<LeaveRoomService>();
builder.Services.AddScoped<ClueService>();

var app = builder.Build();

// ── Security Middleware Pipeline ──
app.UseCors("GameCorsPolicy");
app.UseRateLimiter();
app.UseAntiforgery();

// Serve static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// Swagger hanya di development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();