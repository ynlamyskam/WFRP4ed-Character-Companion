using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Helpers;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Services;

namespace WFRP_Character_Companion.Pages.Characters
{
    [Authorize]
    public class DetailsModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, TalentRulesService talentRules) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        public Character Character { get; set; } = default!;
        public List<Skill> AllSkills { get; set; } = [];
        public TalentRulesService TalentRules { get; set; } = talentRules;
        public int MaxWounds { get; set; }
        public int MaxEncumbrance { get; set; }
        public int MaxCorruption { get; set; }
        public bool IsOwner { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var result = await LoadPageData(id);
            return result ?? Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var character = await _db.Characters
                .Include(c => c.Attributes)
                .Include(c => c.Skills)
                .Include(c => c.Talents)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);

            if (character == null) return NotFound();

            var links = await _db.Set<CampaignCharacter>().Where(cc => cc.CharacterId == id).ToListAsync();
            _db.Set<CampaignCharacter>().RemoveRange(links);
            _db.Characters.Remove(character);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Characters/CharacterHub");
        }

        private async Task<IActionResult?> LoadPageData(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            Character = await _db.Characters
                .Include(c => c.Attributes)
                .Include(c => c.Skills).ThenInclude(cs => cs.Skill)
                .Include(c => c.Talents).ThenInclude(ct => ct.Talent).ThenInclude(t => t.TestEffects)
                .FirstOrDefaultAsync(c => c.Id == id) ?? default!;

            if (Character == null || Character.Id == 0)
                return NotFound();

            IsOwner = Character.UserId == user.Id;
            AllSkills = await _db.Skills.ToListAsync();
            MaxWounds = CharacterRulesHelper.GetMaxWounds(Character);
            MaxEncumbrance = CharacterRulesHelper.GetMaxEncumbrance(Character);
            MaxCorruption = CharacterRulesHelper.GetMaxCorruption(Character);
            return null;
        }

        public int GetSkillAdvances(int skillId, string? specialization = null)
        {
            return Character.Skills
                .Where(cs => cs.SkillId == skillId &&
                    string.Equals(cs.Specialization ?? "", specialization ?? "", StringComparison.OrdinalIgnoreCase))
                .Sum(cs => cs.Advances);
        }
    }
}
