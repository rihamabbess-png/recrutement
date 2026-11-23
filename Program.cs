using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Recrutement.Data;
using Recrutement.Models;
using Recrutement.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Client HTTP typé pour OpenRouter (enregistre aussi MistralService)
builder.Services.AddHttpClient<MistralService>(client =>
{
    client.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
});

builder.Services.AddHostedService<ApplicationAnalysisWorker>();

// Filtre global : redirige vers le changement de mot de passe si nécessaire
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<MustChangePasswordFilter>();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Page d'accueil : la liste des candidatures (le projet n'a pas de HomeController)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Applications}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

app.Run();