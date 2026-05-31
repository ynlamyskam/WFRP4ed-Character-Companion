using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CharacterHubModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public CharacterHubModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public List<Character> Characters { get; set; } = new();

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            Characters = await _db.Characters
                .Where(c => c.UserId == user.Id)
                .ToListAsync();
        }
    }

}
