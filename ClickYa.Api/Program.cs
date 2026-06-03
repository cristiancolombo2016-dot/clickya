using ClickYa.Api;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// 👇 CONFIGURACIÓN KESTREL
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5191);
    options.Limits.MaxRequestBodySize = 52428800;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTodo",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("PermitirTodo");

app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();