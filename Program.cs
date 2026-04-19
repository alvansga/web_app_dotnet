using CodenameApp.Application.Interfaces;
using CodenameApp.Application.Services;
using CodenameApp.Infrastructure.Data;
using CodenameApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "codename.db");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// builder.Services.AddDbContext<AppDbContext>(options =>
//     // options.UseSqlServer("your_connection_string"));
//     options.UseSqlite("Data Source=codename.db"));

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

// Add CORS for frontend requests
app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// Serve static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();