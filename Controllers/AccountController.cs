namespace Recrutement.Controllers;

public class AccountController(
    SignInManager<ApplicationUser> signIn,
    UserManager<ApplicationUser> users) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
        => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await signIn.PasswordSignInAsync(model.Email, model.Password, false, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            var user = await users.FindByEmailAsync(model.Email);
            if (user is not null && user.MustChangePassword)
                return RedirectToAction(nameof(ChangePassword));

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);

            return RedirectToAction("Index", "Applications");
        }

        ModelState.AddModelError(string.Empty, "Courriel ou mot de passe invalide.");
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
        => View(new ChangePasswordViewModel());

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await users.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        var result = await users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        user.MustChangePassword = false;
        await users.UpdateAsync(user);
        await signIn.RefreshSignInAsync(user);

        TempData["Success"] = "Votre mot de passe a été modifié.";
        return RedirectToAction("Index", "Applications");
    }

    public IActionResult AccessDenied() => View();
}