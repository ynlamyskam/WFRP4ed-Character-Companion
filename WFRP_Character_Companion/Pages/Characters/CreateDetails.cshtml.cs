using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Helpers;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Services.CharacterCreation;
using WFRP_Character_Companion.Services.Content;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateDetailsModel(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        CharacterDraftService draftService,
        TalentSyncService talentSync,
        IWebHostEnvironment env) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly CharacterDraftService _draftService = draftService;
        private readonly TalentSyncService _talentSync = talentSync;
        private readonly IWebHostEnvironment _env = env;

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
            var talentsPath = Path.Combine(_env.ContentRootPath, "Data", "Seed", "Content", "talents.json");
            _talentSync.SyncFromFile(talentsPath);
            var allTalents = await _db.Talents.ToListAsync();

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
            character.ExperienceEarned = draft.Experience;
            character.ExperienceSpent = 0;
            character.CorruptionPoints = 0;

            foreach (var key in state.Keys.Where(k => k.StartsWith("Advance_")))
            {
                var rawKey = key["Advance_".Length..];
                if (attributeNames.Contains(rawKey))
                    continue;

                var (skillName, spec) = CharacterRulesHelper.ParseSkillStateKey(rawKey);
                var skill = skills.FirstOrDefault(s => string.Equals(s.Name, skillName, StringComparison.OrdinalIgnoreCase));
                if (skill == null) continue;

                var val = DraftStateHelper.GetInt(state, key);
                var existing = character.Skills.FirstOrDefault(cs =>
                    cs.SkillId == skill.Id &&
                    string.Equals(cs.Specialization ?? "", spec ?? "", StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                    existing.Advances += val;
                else
                    character.Skills.Add(new CharacterSkill { SkillId = skill.Id, Advances = val, Specialization = spec });
            }

            var originTalents = DraftStateHelper.GetTalentEntries(state, "OriginTalents");
            var professionTalent = DraftStateHelper.GetString(state, "ProfessionTier1Talent");
            if (!string.IsNullOrEmpty(professionTalent))
            {
                var (pn, ps) = CreateRaceSkillsAndTalentsModel.ParseTalentKey(professionTalent);
                originTalents.Add(new DraftStateHelper.TalentEntry(pn, ps));
            }

            foreach (var tn in originTalents
                         .GroupBy(t => (CharacterRulesHelper.NormalizeName(t.Name), t.Specialization ?? ""))
                         .Select(g => g.First()))
            {
                var t = _talentSync.FindTalent(allTalents, tn.Name);
                if (t != null)
                {
                    character.Talents.Add(new CharacterTalent
                    {
                        TalentId = t.Id,
                        Level = 1,
                        Specialization = tn.Specialization
                    });
                }
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
