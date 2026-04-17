using ExcellOnServices.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// === 1. CONNECTION STRING UPDATION ===
// Yahan hum wahi connection string use karenge jo DatabaseHandler mein hai
var connectionString = @"Server=.\LAB;Database=ExcellOnDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// === 2. IDENTITY CONFIGURATION (RoleManager Fix) ===
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>() // Ye line add ki hai RoleManager ka error khatam karne ke liye
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.LogoutPath = "/Identity/Account/Logout";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Landing}/{id?}");

app.MapRazorPages();

app.MapControllerRoute(
    name: "dashboard",
    pattern: "dashboard",
    defaults: new { controller = "Home", action = "Index" });

// === 3. SEED DATA & SINGLETON TEST ===
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    
    // Yahan DbInitializer chalega aur Singleton ka pehla instance banayega
    DbInitializer.Initialize(services);

    await CreateDefaultUser(services);
}

// Singleton verification line
var testInstance = ExcellOnServices.Data.DatabaseHandler.GetContext(null);

app.Run();

async Task CreateDefaultUser(IServiceProvider serviceProvider)
{
    try
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        var adminUser = await userManager.FindByEmailAsync("admin@excellon.com");
        if (adminUser == null)
        {
            var user = new IdentityUser
            {
                UserName = "admin@excellon.com",
                Email = "admin@excellon.com",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating default user: {ex.Message}");
    }
}