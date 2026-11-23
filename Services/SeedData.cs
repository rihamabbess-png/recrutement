namespace Recrutement.Services;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider sp)
    {
        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var config = sp.GetRequiredService<IConfiguration>();

        foreach (var role in new[] { "Admin", "RH" })
        {
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));
        }

        var email = config["Seed:AdminEmail"];
        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return;

        if (await users.FindByEmailAsync(email) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = "Administrateur",
                EmailConfirmed = true,
                MustChangePassword = false
            };
            var result = await users.CreateAsync(admin, password);
            if (result.Succeeded)
                await users.AddToRoleAsync(admin, "Admin");
        }
    }
}