using IncredibleComponentsPOE.Data;
using IncredibleComponentsPoe.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();



// EF Core
builder.Services.AddDbContext<IncredibleComponentPoe>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cookie Authentication with role support
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";       // redirect if not logged in
        options.AccessDeniedPath = "/Account/Login"; // redirect if role denied
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add your custom services
builder.Services.AddSingleton<BlobService>();
builder.Services.AddScoped(_ => new AzureFileShareServices(
    builder.Configuration["Azure:ConnectionString"],
    builder.Configuration["Azure:FileShareName"]
));

builder.Services.AddSingleton<QueueService>(sp =>
{

    var config = sp.GetRequiredService<IConfiguration>();
    return new QueueService(config.GetConnectionString("AzureQueueStorage"), "processingorders");
});

var app = builder.Build();

// Middleware order is important!
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
