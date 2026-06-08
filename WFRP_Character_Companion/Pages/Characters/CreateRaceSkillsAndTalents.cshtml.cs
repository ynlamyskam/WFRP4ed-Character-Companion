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
    public class CreateRaceSkillsAndTalentsModel(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env,
        CharacterDraftService draftService,
        CreationContentService content,
        TalentSyncService talentSync) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IWebHostEnvironment _env = env;
        private readonly CharacterDraftService _draftService = draftService;
        private readonly CreationContentService _content = content;
        private readonly TalentSyncService _talentSync = talentSync;

        public string OriginName { get; set; } = string.Empty;
        public List<TalentOption> FixedTalents { get; set; } = [];
        public List<List<TalentOption>> ChoiceTalentGroups { get; set; } = [];
        public List<string> RandomTalents { get; set; } = [];
        public List<SkillOption> OriginSkills { get; set; } = [];

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
            public string Key { get; set; } = string.Empty;
            public string Display { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Specialization { get; set; }
            public bool AllowsCustomSpecialization { get; set; }

            public static TalentOption FromRef(TalentRef r)
            {
                var custom = CharacterRulesHelper.IsCustomSpecializationPlaceholder(r.Specialization);
                return new TalentOption
                {
                    Name = r.Name,
                    Specialization = custom ? null : r.Specialization,
                    AllowsCustomSpecialization = custom,
                    Key = custom ? r.Name : TalentKey(r.Name, r.Specialization),
                    Display = custom
                        ? $"{r.Name} (wpisz specjalizację)"
                        : CharacterRulesHelper.FormatWithSpecialization(r.Name, r.Specialization)
                };
            }

            public static TalentOption FromName(string name) => new()
            {
                Name = name,
                Key = name,
                Display = name
            };
        }

        public static string TalentKey(string name, string? spec) =>
            string.IsNullOrWhiteSpace(spec) ? name : $"{name}::{spec}";

        public static (string Name, string? Spec) ParseTalentKey(string key)
        {
            var sep = key.IndexOf("::", StringComparison.Ordinal);
            if (sep < 0) return (key, null);
            return (key[..sep], key[(sep + 2)..]);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            await LoadOriginDataAsync(draft.Origin ?? string.Empty);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            await LoadOriginDataAsync(draft.Origin ?? string.Empty);

            var resolvedTalents = new List<DraftStateHelper.TalentEntry>();

            for (int i = 0; ; i++)
            {
                var key = $"talent_choice_{i}";
                if (!Request.Form.ContainsKey(key)) break;
                var v = Request.Form[key].ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(v)) continue;

                var (n, s) = ParseTalentKey(v);
                if (ChoiceTalentGroups.Count > i)
                {
                    var match = ChoiceTalentGroups[i].FirstOrDefault(o => o.Name == n);
                    if (match?.AllowsCustomSpecialization == true)
                    {
                        var custom = Request.Form[$"talent_custom_spec_{i}"].ToString()?.Trim();
                        if (string.IsNullOrEmpty(custom))
                            return this.PageWithError("Wpisz specjalizację dla wybranego talentu.");
                        s = custom;
                    }
                }
                resolvedTalents.Add(new DraftStateHelper.TalentEntry(n, s));
            }

            foreach (var ft in FixedTalents)
            {
                string? spec = ft.Specialization;
                if (ft.AllowsCustomSpecialization)
                {
                    var custom = Request.Form[$"fixed_talent_spec_{ft.Name}"].ToString()?.Trim();
                    if (string.IsNullOrEmpty(custom))
                        return this.PageWithError($"Wpisz specjalizację dla talentu {ft.Name}.");
                    spec = custom;
                }
                resolvedTalents.Add(new DraftStateHelper.TalentEntry(ft.Name, spec));
            }

            if (RandomTalents.Any())
            {
                var allTalents = await _db.Talents.ToListAsync();
                foreach (var rt in RandomTalents)
                {
                    var match = _talentSync.FindTalent(allTalents, rt);
                    if (match != null)
                        resolvedTalents.Add(new DraftStateHelper.TalentEntry(match.Name, null));
                }
            }

            var plus3 = new List<string>();
            var plus5 = new List<string>();
            for (int i = 0; i < OriginSkills.Count; i++)
            {
                var bonusKey = $"skill_bonus_{i}";
                if (!Request.Form.ContainsKey(bonusKey)) continue;
                var bonus = Request.Form[bonusKey].ToString();
                if (bonus != "3" && bonus != "5") continue;

                var opt = OriginSkills[i];
                string? spec = opt.Specialization;
                if (opt.AllowsCustomSpecialization)
                {
                    spec = Request.Form[$"skill_custom_spec_{i}"].ToString()?.Trim();
                    if (string.IsNullOrEmpty(spec))
                        return this.PageWithError($"Wpisz specjalizację dla umiejętności {opt.Name}.");
                }

                var skillKey = CharacterRulesHelper.SkillStateKey(opt.Name, spec);
                if (bonus == "3") plus3.Add(skillKey);
                else plus5.Add(skillKey);
            }

            if (plus3.Count > 3)
                return this.PageWithError("Możesz wybrać maksymalnie 3 umiejętności na +3.");
            if (plus5.Count > 3)
                return this.PageWithError("Możesz wybrać maksymalnie 3 umiejętności na +5.");
            if (plus3.Intersect(plus5, StringComparer.OrdinalIgnoreCase).Any())
                return this.PageWithError("Ta sama umiejętność nie może mieć jednocześnie +3 i +5.");

            var skillAdvances = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var s in plus3)
                skillAdvances[s] = skillAdvances.GetValueOrDefault(s) + 3;
            foreach (var s in plus5)
                skillAdvances[s] = skillAdvances.GetValueOrDefault(s) + 5;

            var state = DraftStateHelper.Parse(draft.StateJson);
            var distinctTalents = resolvedTalents
                .GroupBy(t => (CharacterRulesHelper.NormalizeName(t.Name), t.Specialization ?? ""))
                .Select(g => g.First())
                .Select(t => new { t.Name, t.Specialization })
                .ToList();
            DraftStateHelper.SetValue(state, "OriginTalents", distinctTalents);
            DraftStateHelper.SetValue(state, "OriginSkillsPlus3", plus3);
            DraftStateHelper.SetValue(state, "OriginSkillsPlus5", plus5);
            foreach (var kv in skillAdvances)
                DraftStateHelper.SetValue(state, $"Advance_{kv.Key}", kv.Value);

            draft.StateJson = DraftStateHelper.Serialize(state);
            draft.Step = CharacterCreationStep.ProfessionSkillsAndTalents;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateProfessionSkillsAndTalents");
        }

        private async Task LoadOriginDataAsync(string originName)
        {
            OriginName = originName;
            FixedTalents = [];
            ChoiceTalentGroups = [];
            RandomTalents = [];
            OriginSkills = [];

            var all = await _content.LoadOriginsAsync();
            var origin = FindOrigin(all, originName);
            if (origin == null) return;

            foreach (var t in origin.Package.Talents)
            {
                if (t.Type == TalentGrantType.Fixed && t.Talent != null)
                    FixedTalents.Add(TalentOption.FromRef(t.Talent));
                else if (t.Type == TalentGrantType.Choice && t.Options != null)
                    ChoiceTalentGroups.Add(t.Options.Select(TalentOption.FromRef).ToList());
                else if (t.Type == TalentGrantType.Random)
                {
                    var rpath2 = Path.Combine(_env.ContentRootPath, "Data", "Seed", "Content", "RandomTalents.json");
                    if (System.IO.File.Exists(rpath2))
                    {
                        var pool = JsonSerializer.Deserialize<List<string>>(await System.IO.File.ReadAllTextAsync(rpath2)) ?? [];
                        var count = t.Count > 0 ? t.Count : 1;
                        RandomTalents.AddRange(pool.OrderBy(_ => Random.Shared.Next()).Take(count));
                    }
                }
            }

            var skillKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var s in origin.Package.Skills)
            {
                if (s.Skill != null)
                    AddSkillOption(s.Skill.Name, s.Skill.Specialization, skillKeys);
                else if (s.Options != null)
                {
                    foreach (var o in s.Options)
                        AddSkillOption(o.Name, o.Specialization, skillKeys);
                }
            }
        }

        private void AddSkillOption(string name, string? specialization, HashSet<string> skillKeys)
        {
            var custom = CharacterRulesHelper.IsCustomSpecializationPlaceholder(specialization);
            var spec = custom ? null : specialization;
            var key = custom ? name : CharacterRulesHelper.SkillStateKey(name, spec);
            if (!skillKeys.Add(key)) return;

            OriginSkills.Add(new SkillOption
            {
                Name = name,
                Specialization = spec,
                AllowsCustomSpecialization = custom,
                Key = key,
                Display = custom
                    ? name
                    : CharacterRulesHelper.FormatWithSpecialization(name, spec)
            });
        }

        private static Origin? FindOrigin(List<Origin> all, string originName) =>
            all.FirstOrDefault(o =>
                !string.IsNullOrEmpty(o.Name) &&
                (string.Equals(o.Name, originName, StringComparison.OrdinalIgnoreCase) ||
                 CreationContentService.Normalize(o.Name) == CreationContentService.Normalize(originName)));
    }
}
