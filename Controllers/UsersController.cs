namespace Recrutement.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController(UserManager<ApplicationUser> users, ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var rows = new List<(ApplicationUser User, IList<string> Roles)>();
        foreach (var u in users.Users.OrderBy(u => u.FullName).ToList())
            rows.Add((u, await users.GetRolesAsync(u)));

        ViewBag.CurrentUserId = users.GetUserId(User);
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string fullName, string email, string role)
    {
        if (role is not ("RH" or "Admin"))
        {
            TempData["Error"] = "Rôle invalide.";
            return RedirectToAction(nameof(Index));
        }

        var password = PasswordGenerator.Generate();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true,
            MustChangePassword = true
        };

        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        await users.AddToRoleAsync(user, role);

        // Le mot de passe est affiché une seule fois, à l'admin
        TempData["NewUserEmail"] = email;
        TempData["NewUserPassword"] = password;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();

        var password = PasswordGenerator.Generate();
        var token = await users.GeneratePasswordResetTokenAsync(user);
        await users.ResetPasswordAsync(user, token, password);

        TempData["NewUserEmail"] = user.Email;
        TempData["NewUserPassword"] = password;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var currentId = users.GetUserId(User);

        // Personne ne peut supprimer son propre compte
        if (id == currentId)
        {
            TempData["Error"] = "Vous ne pouvez pas supprimer votre propre compte.";
            return RedirectToAction(nameof(Index));
        }

        // Défense en profondeur : seul un admin peut supprimer un compte
        if (!User.IsInRole("Admin")) return Forbid();

        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();

        // Réassigner les offres créées par ce compte à l'admin qui supprime
        await db.JobOffers
            .Where(j => j.CreatedById == id)
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.CreatedById, currentId!));

        // Retirer les références de révision, pour éviter la contrainte de clé étrangère
        await db.JobApplications
            .Where(a => a.ReviewedById == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.ReviewedById, (string?)null));

        var result = await users.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = $"Le compte {user.Email} a été supprimé. Ses offres vous ont été réassignées.";
        return RedirectToAction(nameof(Index));
    }
}