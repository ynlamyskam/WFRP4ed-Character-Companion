using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Services;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class DetailsModel(ApplicationDbContext db, TalentRulesService talentRules) : PageModel
    {
        private readonly ApplicationDbContext _db = db;

        public Character Character { get; set; } = default!;

        public Dictionary<int, CharacterSkill> SkillLookup { get; set; } = [];
        public Dictionary<AttributeType, CharacterAttribute> AttributeLookup { get; set; } = [];
        public List<Skill> AllSkills { get; set; } = [];
        public TalentRulesService TalentRules { get; set; } = talentRules;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Character = await _db.Characters
                .Include(c => c.Attributes)
                .Include(c => c.Skills)
                    .ThenInclude(cs => cs.Skill)
                .Include(c => c.Talents)
                    .ThenInclude(ct => ct.Talent)
                        .ThenInclude(t => t.TestEffects)
                .FirstOrDefaultAsync(c => c.Id == id);

            AllSkills = await _db.Skills.ToListAsync();

            SkillLookup = Character.Skills.ToDictionary(x => x.SkillId);
            AttributeLookup = Character.Attributes.ToDictionary(x => x.Type);

            if (Character == null)
                return NotFound();

            return Page();
        }
    }
}
