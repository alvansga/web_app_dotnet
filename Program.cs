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
builder.Services.AddDbContext<AppDbContext>(options =>
    // options.UseSqlServer("your_connection_string"));
    options.UseSqlite("Data Source=codename.db"));

builder.Services.AddScoped<ICodenameRepository, CodenameRepository>();
builder.Services.AddScoped<CreateCodenameService>();
builder.Services.AddScoped<GetCodenameService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();