using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateRaceSkillsAndTalentsModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IWebHostEnvironment _env = env;

        public string OriginName { get; set; } = string.Empty;
        public List<string> FixedTalents { get; set; } = new();
        public List<List<string>> ChoiceTalentGroups { get; set; } = new();
        public List<string> RandomTalents { get; set; } = new();

        public List<string> OriginSkills { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var draft = await GetOrCreateDraft();
            var originName = draft.Origin ?? string.Empty;
            OriginName = originName;

            // load origins from Content or Data/Seed/Content origins*.json
            var contentDir1 = Path.Combine(_env.ContentRootPath, "Content");
            var contentDir2 = Path.Combine(_env.ContentRootPath, "Data", "Seed", "Content");
            var originFiles = new List<string>();
            if (Directory.Exists(contentDir1)) originFiles.AddRange(Directory.GetFiles(contentDir1, "origins*.json"));
            if (Directory.Exists(contentDir2)) originFiles.AddRange(Directory.GetFiles(contentDir2, "origins*.json"));

            var all = new List<Origin>();
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            opts.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            foreach (var f in originFiles.OrderBy(f => f))
            {
                var txt = await System.IO.File.ReadAllTextAsync(f);
                var list = JsonSerializer.Deserialize<List<Origin>>(txt, opts);
                if (list != null) all.AddRange(list);
            }


            var origin = all.FirstOrDefault(o => !string.IsNullOrEmpty(o.Name) && string.Equals(o.Name, originName, StringComparison.OrdinalIgnoreCase));
            if (origin == null && !string.IsNullOrEmpty(originName))
            {
                var norm = Normalize(originName);
                origin = all.FirstOrDefault(o => !string.IsNullOrEmpty(o.Name) && Normalize(o.Name) == norm);
            }
            if (origin != null)
            {
                // talents
                foreach (var t in origin.Package.Talents)
                {
                    if (t.Type == TalentGrantType.Fixed && t.Talent != null)
                        FixedTalents.Add(t.Talent.Name);
                    else if (t.Type == TalentGrantType.Choice && t.Options != null)
                        ChoiceTalentGroups.Add(t.Options.Select(o => o.Name).ToList());
                    else if (t.Type == TalentGrantType.Random)
                    {
                        // load RandomTalents.json (full pool) and pick required count
                        var rpath1 = Path.Combine(_env.ContentRootPath, "Content", "RandomTalents.json");
                        var rpath2 = Path.Combine(_env.ContentRootPath, "Data", "Seed", "Content", "RandomTalents.json");
                        string? rtxt = null;
                        if (System.IO.File.Exists(rpath1)) rtxt = await System.IO.File.ReadAllTextAsync(rpath1);
                        else if (System.IO.File.Exists(rpath2)) rtxt = await System.IO.File.ReadAllTextAsync(rpath2);
                        var pool = !string.IsNullOrEmpty(rtxt) ? JsonSerializer.Deserialize<List<string>>(rtxt) ?? new() : new List<string>();
                        var count = t.Count > 0 ? t.Count : 1;
                        var picks = pool.OrderBy(_ => Random.Shared.Next()).Take(count).ToList();
                        RandomTalents.AddRange(picks);
                    }
                }

                // skills
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
            var draft = await GetOrCreateDraft();
            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();

            // chosen talents
            var chosenTalents = new List<string>();
            for (int i = 0; ; i++)
            {
                var key = $"talent_choice_{i}";
                if (!Request.Form.ContainsKey(key)) break;
                var v = Request.Form[key].ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(v)) chosenTalents.Add(v);
            }

            // skills +3 and +5
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

            // validate not more than 6 distinct skills and no duplicates between +3 and +5
            var nonEmptyPlus3 = plus3.Where(s => !string.IsNullOrEmpty(s)).ToList();
            var nonEmptyPlus5 = plus5.Where(s => !string.IsNullOrEmpty(s)).ToList();
            var overlap = nonEmptyPlus3.Intersect(nonEmptyPlus5).ToList();
            if (overlap.Any())
                return BadRequest("Ta sama umiejętność nie może być jednocześnie wybrana na +3 i +5.");

            var distinct = nonEmptyPlus3.Concat(nonEmptyPlus5).Distinct().ToList();
            if (distinct.Count > 6)
                return BadRequest("Możesz przydzielić bonusy maksymalnie do 6 różnych umiejętności.");

            // resolve random talents to real talents from DB when needed
            var resolvedTalents = new List<string>();
            if (chosenTalents.Any()) resolvedTalents.AddRange(chosenTalents);

            // add fixed talents
            if (FixedTalents.Any()) resolvedTalents.AddRange(FixedTalents);

            // handle RandomTalents resolution: map names from RandomTalents.json to Talent entities in DB
            if (RandomTalents.Any())
            {
                var allTalents = await _db.Talents.ToListAsync();
                foreach (var rt in RandomTalents)
                {
                    var match = allTalents.FirstOrDefault(t => string.Equals(t.Name, rt, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        resolvedTalents.Add(match.Name);
                }
            }

            // store skill advances as Advance_<SkillName> keys
            var skillAdvances = new Dictionary<string, int>();
            foreach (var s in nonEmptyPlus3)
                skillAdvances[s] = skillAdvances.GetValueOrDefault(s) + 3;
            foreach (var s in nonEmptyPlus5)
                skillAdvances[s] = skillAdvances.GetValueOrDefault(s) + 5;

            state["OriginTalents"] = resolvedTalents.Distinct().ToList();
            state["OriginSkillsPlus3"] = nonEmptyPlus3;
            state["OriginSkillsPlus5"] = nonEmptyPlus5;
            // persist advances under Advance_<SkillName>
            foreach (var kv in skillAdvances)
            {
                state[$"Advance_{kv.Key}"] = kv.Value;
            }

            draft.StateJson = JsonSerializer.Serialize(state);
            draft.Step = CharacterCreationStep.ProfessionSkillsAndTalents;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateProfessionSkillsAndTalents");
        }

        private async Task<CharacterDraft> GetOrCreateDraft()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.RaceSkillsAndTalents);
            if (draft != null)
                return draft;
            draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.RaceSkillsAndTalents, Experience = 0 };
            _db.CharacterDrafts.Add(draft);
            await _db.SaveChangesAsync();
            return draft;
        }

        private static string Normalize(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var form = s.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in form)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant().Replace(" ", "");
        }
    }
}
