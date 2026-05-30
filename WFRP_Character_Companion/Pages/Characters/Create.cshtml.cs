using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;

namespace WFRP_Character_Companion.Pages.Characters
{
    [Authorize]
    public class CreateModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        [BindProperty]
        public Character Input { get; set; } = new Character();

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            
            if (user == null)
                return Unauthorized();
        
            Input.UserId = user.Id;

            _db.Characters.Add(Input);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Characters/CharacterHub");
        }
    }
}
