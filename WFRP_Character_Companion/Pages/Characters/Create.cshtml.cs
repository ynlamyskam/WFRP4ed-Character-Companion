using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace WFRP_Character_Companion.Pages.Characters
{
    [Authorize]
    public class CreateModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        [BindProperty]
        public CreateCharacterViewModel Input { get; set; } = new();

        public List<Skill> AllSkills { get; set; } = [];
        public List<Talent> AllTalents { get; set; } = [];

        public async Task OnGetAsync()
        {
            AllSkills = await _db.Skills
                .OrderBy(s => s.Name)
                .ToListAsync();

            AllTalents = await _db.Talents
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var character = new Character
            {
                Name = Input.Name,
                UserId = user.Id,

                Attributes = Input.Attributes.Select(a => new CharacterAttribute
                {
                    Type = a.Type,
                    Basic = a.Basic,
                    Advance = a.Advance
                }).ToList(),

                Skills = Input.Skills
                    .Where(s => s.SkillId > 0)
                    .Select(s => new CharacterSkill
                    {
                        SkillId = s.SkillId,
                        Advances = s.Advances
                    })
                    .ToList(),

                Talents = Input.Talents
                    .Where(t => t.TalentId > 0)
                    .Select(t => new CharacterTalent
                    {
                        TalentId = t.TalentId,
                        Level = t.Level
                    })
                    .ToList()
            };

            _db.Characters.Add(character);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Characters/CharacterHub");
        }
    }
}
