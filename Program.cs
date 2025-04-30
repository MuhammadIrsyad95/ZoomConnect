using Microsoft.EntityFrameworkCore;
using Zoom.Models;
using Zoom.Services.Interfaces;
using Zoom.Services;
using Zoom.Helper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ZoomContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Zoom"));
});

builder.Services.Configure<LDAPSetting>(builder.Configuration.GetSection("LDAPConnection"));
builder.Services.Configure<AdditionalSetting>(builder.Configuration.GetSection("AdditionalConfig"));
builder.Services.AddScoped<LdapAuthClient>(); // Register LdapAuthClient
builder.Services.AddScoped<IEmailServiceSmtp, EmailServiceSmtp>();
builder.Services.AddScoped<IDbInitializer, DbInitializer>();

builder.Services.AddHttpContextAccessor(); // <-- Add this line
builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
builder.Services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
// Add session services
builder.Services.AddSession(); // Add session services

// Add cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Path to redirect for login if not authenticated
        options.ExpireTimeSpan = TimeSpan.FromDays(30); // Cookie expiration time
    });

// Add memory cache
builder.Services.AddMemoryCache();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
SeedDatabase();


// Ensure session middleware is added to the request pipeline
app.UseSession(); // This must be added before UseEndpoints or other middleware that requires sessions

// Configure default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();

void SeedDatabase()
{
    using (var scope = app.Services.CreateScope())
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        dbInitializer.Initialize();
    }
}
