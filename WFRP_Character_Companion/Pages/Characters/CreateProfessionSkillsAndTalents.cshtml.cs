using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Helpers;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Services.CharacterCreation;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateProfessionSkillsAndTalentsModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, CharacterDraftService draftService, CreationContentService content) : PageModel
    {
        public class SkillOption
        {
            public string Name { get; set; } = string.Empty;
            public string? Specialization { get; set; }
            public string Key { get; set; } = string.Empty;
            public string Display { get; set; } = string.Empty;
            public bool AllowsCustomSpecialization { get; set; }
        }

        public class TalentOption
        {
            public string Name { get; set; } = string.Empty;
            public string? Specialization { get; set; }
            public string Key { get; set; } = string.Empty;
            public string Display { get; set; } = string.Empty;
            public bool AllowsCustomSpecialization { get; set; }
        }

        private static SkillOption SkillFromRaw(string raw)
        {
            var (name, spec) = ParseNameAndSpec(raw);
            var custom = CharacterRulesHelper.IsCustomSpecializationPlaceholder(spec);
            return new SkillOption
            {
                Name = name,
                Specialization = custom ? null : spec,
                AllowsCustomSpecialization = custom,
                Key = CharacterRulesHelper.SkillStateKey(name, custom ? null : spec),
                Display = custom ? name : CharacterRulesHelper.FormatWithSpecialization(name, spec)
            };
        }

        private static TalentOption TalentFromRaw(string raw)
        {
            var (name, spec) = ParseNameAndSpec(raw);
            var custom = CharacterRulesHelper.IsCustomSpecializationPlaceholder(spec);
            return new TalentOption
            {
                Name = name,
                Specialization = custom ? null : spec,
                AllowsCustomSpecialization = custom,
                Key = CreateRaceSkillsAndTalentsModel.TalentKey(name, custom ? null : spec),
                Display = custom ? name : CharacterRulesHelper.FormatWithSpecialization(name, spec)
            };
        }

        private static (string Name, string? Spec) ParseNameAndSpec(string raw)
        {
            var open = raw.LastIndexOf('(');
            var close = raw.EndsWith(')') ? raw.Length - 1 : -1;
            if (open > 0 && close > open)
            {
                var name = raw[..open].Trim();
                var spec = raw[(open + 1)..close].Trim();
                return (name, spec);
            }
            return (raw, null);
        }

        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly CharacterDraftService _draftService = draftService;
        private readonly CreationContentService _content = content;

        public List<SkillOption> TierSkills { get; set; } = [];
        public List<TalentOption> TierTalents { get; set; } = [];
        public string FoundProfessionName { get; set; } = string.Empty;
        public bool HasCustomTalentOption { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            await LoadProfessionTierData(draft);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            await LoadProfessionTierData(draft);

            var pts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < TierSkills.Count; i++)
            {
                var opt = TierSkills[i];
                var key = $"pts_{i}";
                var points = Request.Form.ContainsKey(key) && int.TryParse(Request.Form[key], out var v) ? v : 0;

                string? spec = opt.Specialization;
                if (opt.AllowsCustomSpecialization)
                {
                    spec = Request.Form[$"profession_skill_spec_{i}"].ToString()?.Trim();
                    if (string.IsNullOrEmpty(spec))
                        return this.PageWithError($"Wpisz specjalizację dla umiejętności {opt.Name}.");
                }

                var skillKey = CharacterRulesHelper.SkillStateKey(opt.Name, spec);
                pts[skillKey] = points;
            }

            var total = pts.Values.Sum();
            if (TierSkills.Count > 0 && total != 40)
                return this.PageWithError($"Rozdzielono {total} punktów — wymagane dokładnie 40.");

            var chosen = Request.Form.ContainsKey("chosenTalent") ? Request.Form["chosenTalent"].ToString() ?? string.Empty : string.Empty;
            if (TierTalents.Count > 0 && string.IsNullOrEmpty(chosen))
                return this.PageWithError("Wybierz talent z Tier 1 profesji.");

            if (!string.IsNullOrEmpty(chosen))
            {
                var match = TierTalents.FirstOrDefault(t => t.Key == chosen || t.Name == chosen);
                if (match?.AllowsCustomSpecialization == true)
                {
                    var custom = Request.Form["profession_talent_spec"].ToString()?.Trim();
                    if (string.IsNullOrEmpty(custom))
                        return this.PageWithError($"Wpisz specjalizację dla talentu {match.Name}.");
                    chosen = CreateRaceSkillsAndTalentsModel.TalentKey(match.Name, custom);
                }
            }

            var state = DraftStateHelper.Parse(draft.StateJson);
            foreach (var kv in pts)
            {
                var existingKey = $"Advance_{kv.Key}";
                var existing = DraftStateHelper.GetInt(state, existingKey);
                DraftStateHelper.SetValue(state, existingKey, existing + kv.Value);
            }

            if (!string.IsNullOrEmpty(chosen))
                DraftStateHelper.SetValue(state, "ProfessionTier1Talent", chosen);

            draft.StateJson = DraftStateHelper.Serialize(state);
            draft.Step = CharacterCreationStep.Equipment;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateEquipment");
        }

        private async Task LoadProfessionTierData(CharacterDraft draft)
        {
            var professionName = GetProfessionFromDraft(draft);
            FoundProfessionName = professionName ?? string.Empty;
            TierSkills = [];
            TierTalents = [];
            HasCustomTalentOption = false;

            if (string.IsNullOrEmpty(professionName)) return;

            var professions = await _content.LoadProfessionsAsync();
            var prof = _content.FindProfession(professions, professionName);
            if (prof?.Tiers?.Count > 0)
            {
                var tier1 = prof.Tiers[0];
                TierSkills = (tier1.Skills ?? []).Select(SkillFromRaw).ToList();
                TierTalents = (tier1.Talents ?? []).Select(TalentFromRaw).ToList();
                HasCustomTalentOption = TierTalents.Any(t => t.AllowsCustomSpecialization);
            }
        }

        private static string? GetProfessionFromDraft(CharacterDraft draft)
        {
            var state = DraftStateHelper.Parse(draft.StateJson);
            var name = DraftStateHelper.GetString(state, "Profession");
            return string.IsNullOrEmpty(name) ? null : name;
        }
    }
}
