using CodenameApp.Application.Interfaces;
using CodenameApp.Application.Services;
using CodenameApp.Infrastructure.Data;
using CodenameApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer("your_connection_string"));

builder.Services.AddScoped<ICodenameRepository, CodenameRepository>();
builder.Services.AddScoped<CreateCodenameService>();
builder.Services.AddScoped<GetCodenameService>();

var app = builder.Build();

app.MapControllers();

app.Run();