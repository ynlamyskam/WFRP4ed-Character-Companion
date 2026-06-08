using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Models;

namespace WFRP_Character_Companion.Areas.Identity.Pages.Account
{
    public class LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger) : PageModel
    {
        public async Task<IActionResult> OnPost(string? returnUrl = null)
        {
            await signInManager.SignOutAsync();
            logger.LogInformation("User logged out.");
            returnUrl ??= Url.Content("~/");
            return LocalRedirect(returnUrl);
        }
    }
}
