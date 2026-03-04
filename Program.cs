using AssetManager.Data;
using AssetManager.Helpers;
using AssetManager.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// DB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured. Set it via user-secrets (dev) or environment variable (production).");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
           .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

// COOKIE AUTHENTICATION (replaces Identity)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", o =>
    {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

var app = builder.Build();

// SEED DEFAULT ADMIN USER ON STARTUP
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<ApplicationDbContext>();

    try
    {
        // If compiled migrations exist in this assembly, apply them.
        // If not, fall back to EnsureCreated to create schema from the model.
        // This avoids querying a missing table when no migrations are present.
        var migrations = context.Database.GetMigrations();
        if (migrations != null && migrations.Any())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            logger.LogInformation("No EF Core migrations found in assembly; using EnsureCreatedAsync to create database schema.");
            await context.Database.EnsureCreatedAsync();
        }

        // Seed admin user if not exists
        await SeedAdminUserAsync(context, builder.Configuration);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Auth/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();

// Helper method to seed default admin user
async Task SeedAdminUserAsync(ApplicationDbContext context, IConfiguration config)
{
    var adminEmail = config["Admin:Email"]
    ?? throw new InvalidOperationException("Admin:Email is not configured. Set it via environment variable or user-secrets.");
    var adminPassword = config["Admin:Password"]
        ?? throw new InvalidOperationException("Admin:Password is not configured. Set it via environment variable or user-secrets.");

    // Ensure the schema exists before querying the Users DbSet
    if (!await context.Users.AnyAsync(u => u.Email == adminEmail))
    {
        var adminUser = new User
        {
            Email = adminEmail,
            PasswordHash = PasswordHelper.HashPassword(adminPassword),
            Role = Roles.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
    }
}
