using ClickYa.Comercios.Security;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
    throw new InvalidOperationException("Falta Api__BaseUrl para conectar el panel con ClickYa.Api.");
if (!builder.Environment.IsDevelopment() &&
    !apiBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Api__BaseUrl debe usar HTTPS en producción.");

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AllowAnonymousToPage("/Admin/Login");
    options.Conventions.AllowAnonymousToPage("/Comercio/Dashboard");
    options.Conventions.AllowAnonymousToPage("/tecnico/Dashboard");
});

// 👉 NECESARIO PARA IHttpClientFactory (NO BORRAR)
builder.Services.AddHttpClient("ClickYaApi", client => client.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddAuthentication(WebSecurity.CookieScheme)
    .AddCookie(WebSecurity.CookieScheme, options =>
    {
        options.Cookie.Name = "__Host-ClickYaPanel";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/Login";
        options.SlidingExpiration = false;
    });
builder.Services.AddAuthorization(options =>
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(WebSecurity.AdminRole)));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.Map("/secure-api/{**path}", SecureApiProxy.Forward);

app.MapRazorPages();

app.Run();
