using Microsoft.AspNetCore.Mvc.Filters;

namespace Recrutement.Services;

public class MustChangePasswordFilter(UserManager<ApplicationUser> users) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString();

        // Pages autorisées même si le mot de passe doit changer
        bool isAllowed = controller == "Account" && (action == "ChangePassword" || action == "Logout" || action == "Login" || action == "AccessDenied");

        if (context.HttpContext.User.Identity?.IsAuthenticated == true && !isAllowed)
        {
            var user = await users.GetUserAsync(context.HttpContext.User);
            if (user is not null && user.MustChangePassword)
            {
                context.Result = new RedirectToActionResult("ChangePassword", "Account", null);
                return;
            }
        }

        await next();
    }
}