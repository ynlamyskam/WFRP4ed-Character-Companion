using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Models;

namespace WFRP_Character_Companion.Areas.Identity.Pages.Account
{
    public class RegisterModel(
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger) : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IUserStore<ApplicationUser> _userStore = userStore;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly ILogger<RegisterModel> _logger = logger;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Podaj nazwę wyświetlaną.")]
            [StringLength(64)]
            [Display(Name = "Nazwa wyświetlana")]
            public string DisplayName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Podaj adres e-mail.")]
            [EmailAddress(ErrorMessage = "Nieprawidłowy adres e-mail.")]
            [Display(Name = "E-mail")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Podaj hasło.")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Hasło musi mieć co najmniej {2} znaków.")]
            [DataType(DataType.Password)]
            [Display(Name = "Hasło")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Potwierdź hasło.")]
            [DataType(DataType.Password)]
            [Display(Name = "Potwierdź hasło")]
            [Compare("Password", ErrorMessage = "Hasła nie są identyczne.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl ?? Url.Content("~/");

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            if (!ModelState.IsValid)
                return Page();

            var user = new ApplicationUser
            {
                DisplayName = Input.DisplayName.Trim()
            };

            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            var emailStore = (IUserEmailStore<ApplicationUser>)_userStore;
            await emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
            await emailStore.SetEmailConfirmedAsync(user, true, CancellationToken.None);

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account.");
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(ReturnUrl);
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }
    }
}
