using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Helpers;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Services.CharacterCreation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateRaceSkillsAndTalentsModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env, CharacterDraftService draftService, CreationContentService content) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IWebHostEnvironment _env = env;
        private readonly CharacterDraftService _draftService = draftService;
        private readonly CreationContentService _content = content;

        public string OriginName { get; set; } = string.Empty;
        public List<string> FixedTalents { get; set; } = new();
        public List<List<string>> ChoiceTalentGroups { get; set; } = new();
        public List<string> RandomTalents { get; set; } = new();
        public List<string> OriginSkills { get; set; } = new();

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
            var originName = draft.Origin ?? string.Empty;
            await LoadOriginDataAsync(originName);

            var all = await _content.LoadOriginsAsync();
            var origin = FindOrigin(all, originName);

            var fixedTalents = new List<string>();
            var randomTalents = new List<string>();
            if (origin != null)
            {
                foreach (var t in origin.Package.Talents)
                {
                    if (t.Type == TalentGrantType.Fixed && t.Talent != null)
                        fixedTalents.Add(t.Talent.Name);
                    else if (t.Type == TalentGrantType.Random)
                    {
                        var rpath2 = Path.Combine(_env.ContentRootPath, "Data", "Seed", "Content", "RandomTalents.json");
                        if (System.IO.File.Exists(rpath2))
                        {
                            var pool = JsonSerializer.Deserialize<List<string>>(await System.IO.File.ReadAllTextAsync(rpath2)) ?? [];
                            var count = t.Count > 0 ? t.Count : 1;
                            randomTalents.AddRange(pool.OrderBy(_ => Random.Shared.Next()).Take(count));
                        }
                    }
                }
            }

            var chosenTalents = new List<string>();
            for (int i = 0; ; i++)
            {
                var key = $"talent_choice_{i}";
                if (!Request.Form.ContainsKey(key)) break;
                var v = Request.Form[key].ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(v)) chosenTalents.Add(v);
            }

            var plus3 = new List<string>();
            var plus5 = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                var k3 = $"skill_plus3_{i}";
                if (Request.Form.ContainsKey(k3) && !string.IsNullOrEmpty(Request.Form[k3]))
                    plus3.Add(Request.Form[k3].ToString() ?? string.Empty);
                var k5 = $"skill_plus5_{i}";
                if (Request.Form.ContainsKey(k5) && !string.IsNullOrEmpty(Request.Form[k5]))
                    plus5.Add(Request.Form[k5].ToString() ?? string.Empty);
            }

            var nonEmptyPlus3 = plus3.Where(s => !string.IsNullOrEmpty(s)).ToList();
            var nonEmptyPlus5 = plus5.Where(s => !string.IsNullOrEmpty(s)).ToList();

            if (nonEmptyPlus3.Count != nonEmptyPlus3.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                return this.PageWithError("Ta sama umiejętność nie może być wybrana dwa razy w sekcji +3.");
            if (nonEmptyPlus5.Count != nonEmptyPlus5.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                return this.PageWithError("Ta sama umiejętność nie może być wybrana dwa razy w sekcji +5.");
            if (nonEmptyPlus3.Intersect(nonEmptyPlus5, StringComparer.OrdinalIgnoreCase).Any())
                return this.PageWithError("Ta sama umiejętność nie może być jednocześnie wybrana na +3 i +5.");

            var distinct = nonEmptyPlus3.Concat(nonEmptyPlus5).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (distinct.Count > 6)
                return this.PageWithError("Możesz przydzielić bonusy maksymalnie do 6 różnych umiejętności.");

            var resolvedTalents = new List<string>();
            resolvedTalents.AddRange(chosenTalents);
            resolvedTalents.AddRange(fixedTalents);

            if (randomTalents.Any())
            {
                var allTalents = await _db.Talents.ToListAsync();
                foreach (var rt in randomTalents)
                {
                    var match = allTalents.FirstOrDefault(t => string.Equals(t.Name, rt, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        resolvedTalents.Add(match.Name);
                }
            }

            var skillAdvances = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in nonEmptyPlus3)
                skillAdvances[s] = skillAdvances.GetValueOrDefault(s) + 3;
            foreach (var s in nonEmptyPlus5)
                skillAdvances[s] = skillAdvances.GetValueOrDefault(s) + 5;

            var state = DraftStateHelper.Parse(draft.StateJson);
            DraftStateHelper.SetValue(state, "OriginTalents", resolvedTalents.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
            DraftStateHelper.SetValue(state, "OriginSkillsPlus3", nonEmptyPlus3);
            DraftStateHelper.SetValue(state, "OriginSkillsPlus5", nonEmptyPlus5);
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
                    FixedTalents.Add(t.Talent.Name);
                else if (t.Type == TalentGrantType.Choice && t.Options != null)
                    ChoiceTalentGroups.Add(t.Options.Select(o => o.Name).ToList());
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

            foreach (var s in origin.Package.Skills)
            {
                if (s.Skill != null)
                    OriginSkills.Add(s.Skill.Name);
                else if (s.Options != null)
                    OriginSkills.AddRange(s.Options.Select(o => o.Name));
            }
            OriginSkills = OriginSkills.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static Origin? FindOrigin(List<Origin> all, string originName)
        {
            return all.FirstOrDefault(o =>
                !string.IsNullOrEmpty(o.Name) &&
                (string.Equals(o.Name, originName, StringComparison.OrdinalIgnoreCase) ||
                 CreationContentService.Normalize(o.Name) == CreationContentService.Normalize(originName)));
        }
    }
}
