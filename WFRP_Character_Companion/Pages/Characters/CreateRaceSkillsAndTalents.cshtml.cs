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
            var originName = draft.Origin ?? string.Empty;
            OriginName = originName;

            var all = await _content.LoadOriginsAsync();
            var origin = all.FirstOrDefault(o =>
                !string.IsNullOrEmpty(o.Name) &&
                (string.Equals(o.Name, originName, StringComparison.OrdinalIgnoreCase) ||
                 CreationContentService.Normalize(o.Name) == CreationContentService.Normalize(originName)));

            if (origin != null)
            {
                foreach (var t in origin.Package.Talents)
                {
                    if (t.Type == TalentGrantType.Fixed && t.Talent != null)
                        FixedTalents.Add(t.Talent.Name);
                    else if (t.Type == TalentGrantType.Choice && t.Options != null)
                        ChoiceTalentGroups.Add(t.Options.Select(o => o.Name).ToList());
                    else if (t.Type == TalentGrantType.Random)
                    {
                        var rpath1 = Path.Combine(_env.ContentRootPath, "Content", "RandomTalents.json");
                        var rpath2 = Path.Combine(_env.ContentRootPath, "Data", "Seed", "Content", "RandomTalents.json");
                        string? rtxt = null;
                        if (System.IO.File.Exists(rpath1)) rtxt = await System.IO.File.ReadAllTextAsync(rpath1);
                        else if (System.IO.File.Exists(rpath2)) rtxt = await System.IO.File.ReadAllTextAsync(rpath2);
                        var pool = !string.IsNullOrEmpty(rtxt) ? JsonSerializer.Deserialize<List<string>>(rtxt) ?? [] : [];
                        var count = t.Count > 0 ? t.Count : 1;
                        RandomTalents.AddRange(pool.OrderBy(_ => Random.Shared.Next()).Take(count));
                    }
                }

                foreach (var s in origin.Package.Skills)
                {
                    if (s.Skill != null)
                        OriginSkills.Add(s.Skill.Name);
                    else if (s.Options != null)
                        OriginSkills.AddRange(s.Options.Select(o => o.Name));
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();

            // Reload origin data for POST (OnGet doesn't run)
            var all = await _content.LoadOriginsAsync();
            var originName = draft.Origin ?? string.Empty;
            var origin = all.FirstOrDefault(o =>
                !string.IsNullOrEmpty(o.Name) &&
                (string.Equals(o.Name, originName, StringComparison.OrdinalIgnoreCase) ||
                 CreationContentService.Normalize(o.Name) == CreationContentService.Normalize(originName)));

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
            if (nonEmptyPlus3.Intersect(nonEmptyPlus5).Any())
                return BadRequest("Ta sama umiejętność nie może być jednocześnie wybrana na +3 i +5.");

            var distinct = nonEmptyPlus3.Concat(nonEmptyPlus5).Distinct().ToList();
            if (distinct.Count > 6)
                return BadRequest("Możesz przydzielić bonusy maksymalnie do 6 różnych umiejętności.");

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

            var skillAdvances = new Dictionary<string, int>();
            foreach (var s in nonEmptyPlus3)
                skillAdvances[s] = skillAdvances.GetValueOrDefault(s) + 3;
            foreach (var s in nonEmptyPlus5)
                skillAdvances[s] = skillAdvances.GetValueOrDefault(s) + 5;

            state["OriginTalents"] = resolvedTalents.Distinct().ToList();
            state["OriginSkillsPlus3"] = nonEmptyPlus3;
            state["OriginSkillsPlus5"] = nonEmptyPlus5;
            foreach (var kv in skillAdvances)
                state[$"Advance_{kv.Key}"] = kv.Value;

            draft.StateJson = JsonSerializer.Serialize(state);
            draft.Step = CharacterCreationStep.ProfessionSkillsAndTalents;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateProfessionSkillsAndTalents");
        }
    }
}
