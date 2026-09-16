using ClickYa.Api;
using ClickYa.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Falta ConnectionStrings__DefaultConnection. Configurala como variable de entorno.");

var allowedOrigins = (builder.Configuration["Security:AllowedOrigins"] ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
if (!builder.Environment.IsDevelopment())
{
    if (allowedOrigins.Length == 0)
        throw new InvalidOperationException(
            "Falta Security__AllowedOrigins. Indicá los orígenes web permitidos separados por coma.");
    if (string.IsNullOrWhiteSpace(builder.Configuration["Security:SigningKey"]) ||
        builder.Configuration["Security:SigningKey"]!.Length < 32)
        throw new InvalidOperationException("Falta Security__SigningKey o es demasiado corta.");
    if (string.IsNullOrWhiteSpace(builder.Configuration["Security:AdminUsername"]) ||
        !PasswordSecurity.IsHash(builder.Configuration["Security:AdminPasswordHash"]))
        throw new InvalidOperationException(
            "Faltan Security__AdminUsername o Security__AdminPasswordHash válido.");
}

// 👇 CONFIGURACIÓN KESTREL
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5191);
    options.Limits.MaxRequestBodySize = 52428800;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClickYaWeb", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddSingleton<AccessTokenService>();
builder.Services.AddSingleton<WebLoginTicketService>();
builder.Services.AddSingleton<LoginThrottle>();
builder.Services
    .AddAuthentication(SecurityDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, ClickYaAuthenticationHandler>(
        SecurityDefaults.AuthenticationScheme,
        _ => { });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseCors("ClickYaWeb");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
