using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using WFRP_Character_Companion.Models;

namespace WFRP_Character_Companion.Pages
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ProfileModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public ProfileInputModel Input { get; set; } = new ProfileInputModel();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            Input.DisplayName = user.DisplayName;
            Input.CurrentAvatarFileName = user.AvatarFileName;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            user.DisplayName = Input.DisplayName;

            if (Input.AvatarFile != null)
            {
                var ext = Path.GetExtension(Input.AvatarFile.FileName);
                var fileName = $"{user.Id}{ext}";
                var dir = Path.Combine("wwwroot", "avatars");

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var path = Path.Combine(dir, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await Input.AvatarFile.CopyToAsync(stream);
                }

                user.AvatarFileName = fileName;
            }

            await _userManager.UpdateAsync(user);

            await _signInManager.RefreshSignInAsync(user);

            return RedirectToPage();
        }
    }

    public class ProfileInputModel
    {
        public string DisplayName { get; set; } = string.Empty;
        public IFormFile? AvatarFile { get; set; }
        public string? CurrentAvatarFileName { get; set; }
    }
}
