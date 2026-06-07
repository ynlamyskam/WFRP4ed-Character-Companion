using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateDetailsModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        public string Description { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var draft = await GetOrCreateDraft();
            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
            if (state.TryGetValue("Description", out var d))
                Description = d?.ToString() ?? string.Empty;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var draft = await GetOrCreateDraft();
            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();

            var desc = Request.Form.ContainsKey("Description") ? Request.Form["Description"].ToString() ?? string.Empty : string.Empty;
            state["Description"] = desc;

            draft.StateJson = JsonSerializer.Serialize(state);

            // finalize - create Character from draft
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var character = new Character
            {
                Name = state.ContainsKey("Name") ? state["Name"].ToString() ?? "" : "New Character",
                UserId = user.Id,
                Attributes = Enum.GetValues<AttributeType>().Select(a => new CharacterAttribute
                {
                    Type = a,
                    Basic = state.ContainsKey($"Attr_{a}") ? Convert.ToInt32(state[$"Attr_{a}"]) : 0,
                    Advance = state.ContainsKey($"Advance_{a}") ? Convert.ToInt32(state[$"Advance_{a}"]) : 0
                }).ToList(),
                Skills = new List<CharacterSkill>(),
                Talents = new List<CharacterTalent>()
            };

            // personal info
            if (state.TryGetValue("Age", out var age)) character.Age = Convert.ToInt32(age);
            if (state.TryGetValue("Height", out var h)) character.Height = Convert.ToInt32(h);
            if (state.TryGetValue("Weight", out var w)) character.Weight = Convert.ToInt32(w);
            if (state.TryGetValue("EyeColor", out var ec)) character.EyeColor = ec?.ToString() ?? string.Empty;
            if (state.TryGetValue("HairColor", out var hc)) character.HairColor = hc?.ToString() ?? string.Empty;
            if (state.TryGetValue("Description", out var descState)) character.Description = descState?.ToString() ?? string.Empty;

            // apply skill advances from state keys Advance_<SkillName>
            var skills = await _db.Skills.ToListAsync();
            foreach (var key in JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson)!.Keys)
            {
                if (key.StartsWith("Advance_"))
                {
                    var skillName = key.Substring("Advance_".Length);
                    var val = Convert.ToInt32(JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson)![key]);
                    var skill = skills.FirstOrDefault(s => s.Name == skillName);
                    if (skill != null)
                    {
                        character.Skills.Add(new CharacterSkill { SkillId = skill.Id, Advances = val });
                    }
                }
            }

            // apply talents from OriginTalents and ProfessionTier1Talent etc.
            var talentNames = new List<string>();
            if (state.TryGetValue("OriginTalents", out var ot))
            {
                try { talentNames.AddRange(JsonSerializer.Deserialize<List<string>>(ot.ToString() ?? "[]") ?? new()); } catch { }
            }
            if (state.TryGetValue("ProfessionTier1Talent", out var pt))
            {
                if (!string.IsNullOrEmpty(pt?.ToString()))
                    talentNames.Add(pt.ToString()!);
            }

            var talents = await _db.Talents.ToListAsync();
            foreach (var tn in talentNames.Distinct())
            {
                var t = talents.FirstOrDefault(x => x.Name == tn);
                if (t != null)
                {
                    character.Talents.Add(new CharacterTalent { TalentId = t.Id, Level = 1 });
                }
            }

            // items
            if (state.TryGetValue("Items", out var itemsObj))
            {
                try { character.ItemsJson = JsonSerializer.Serialize(JsonSerializer.Deserialize<List<string>>(itemsObj.ToString() ?? "[]") ?? new()); } catch { }
            }

            _db.Characters.Add(character);
            // remove draft
            _db.CharacterDrafts.Remove(draft);

            await _db.SaveChangesAsync();

            return RedirectToPage("/Characters/CharacterHub");
        }

        private async Task<CharacterDraft> GetOrCreateDraft()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.Details);
            if (draft != null)
                return draft;
            draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.Details, Experience = 0 };
            _db.CharacterDrafts.Add(draft);
            await _db.SaveChangesAsync();
            return draft;
        }
    }
}
