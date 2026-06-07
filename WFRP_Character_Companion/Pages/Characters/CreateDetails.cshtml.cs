using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Services.CharacterCreation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateDetailsModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, CharacterDraftService draftService) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly CharacterDraftService _draftService = draftService;

        public string Description { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            var state = DraftStateHelper.Parse(draft.StateJson);
            Description = DraftStateHelper.GetString(state, "Description");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            var state = DraftStateHelper.Parse(draft.StateJson);

            var desc = Request.Form.ContainsKey("Description") ? Request.Form["Description"].ToString() ?? string.Empty : string.Empty;
            DraftStateHelper.SetValue(state, "Description", desc);
            draft.StateJson = DraftStateHelper.Serialize(state);

            var attributeNames = Enum.GetNames<AttributeType>().ToHashSet(StringComparer.OrdinalIgnoreCase);
            var skills = await _db.Skills.ToListAsync();
            var skillNames = skills.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var character = new Character
            {
                Name = DraftStateHelper.GetString(state, "Name", "Nowa postać"),
                UserId = user.Id,
                Attributes = Enum.GetValues<AttributeType>().Select(a => new CharacterAttribute
                {
                    Type = a,
                    Basic = DraftStateHelper.GetInt(state, $"Attr_{a}"),
                    Advance = DraftStateHelper.GetInt(state, $"Advance_{a}")
                }).ToList(),
                Skills = [],
                Talents = []
            };

            character.Age = DraftStateHelper.GetInt(state, "Age");
            character.Height = DraftStateHelper.GetInt(state, "Height");
            character.Weight = DraftStateHelper.GetInt(state, "Weight");
            character.EyeColor = DraftStateHelper.GetString(state, "EyeColor");
            character.HairColor = DraftStateHelper.GetString(state, "HairColor");
            character.Description = desc;

            foreach (var key in state.Keys.Where(k => k.StartsWith("Advance_")))
            {
                var name = key["Advance_".Length..];
                if (attributeNames.Contains(name))
                    continue;
                if (!skillNames.Contains(name))
                    continue;

                var val = DraftStateHelper.GetInt(state, key);
                var skill = skills.First(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
                character.Skills.Add(new CharacterSkill { SkillId = skill.Id, Advances = val });
            }

            var talentNames = DraftStateHelper.GetStringList(state, "OriginTalents");
            var professionTalent = DraftStateHelper.GetString(state, "ProfessionTier1Talent");
            if (!string.IsNullOrEmpty(professionTalent))
                talentNames.Add(professionTalent);

            var talents = await _db.Talents.ToListAsync();
            foreach (var tn in talentNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var t = talents.FirstOrDefault(x => string.Equals(x.Name, tn, StringComparison.OrdinalIgnoreCase));
                if (t != null)
                    character.Talents.Add(new CharacterTalent { TalentId = t.Id, Level = 1 });
            }

            var items = DraftStateHelper.GetStringList(state, "Items");
            if (items.Count > 0)
                character.ItemsJson = JsonSerializer.Serialize(items);

            _db.Characters.Add(character);
            _db.CharacterDrafts.Remove(draft);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Characters/Details", new { id = character.Id });
        }
    }
}
