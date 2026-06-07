using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateProfessionSkillsAndTalentsModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IWebHostEnvironment _env = env;

        public List<string> TierSkills { get; set; } = new();
        public List<string> TierTalents { get; set; } = new();
        public string DebugDraftJson { get; set; } = string.Empty;
        public int LoadedProfessionsCount { get; set; }
        public List<string> LoadedProfessionNames { get; set; } = new();
        public string FoundProfessionName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var draft = await GetOrCreateDraft();
            DebugDraftJson = draft.StateJson;
            var professionName = GetProfessionFromDraft(draft);
            FoundProfessionName = professionName ?? string.Empty;
            if (!string.IsNullOrEmpty(professionName))
            {
                var professions = await LoadAllProfessions();
                LoadedProfessionsCount = professions.Count;
                LoadedProfessionNames = professions.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => p.Name!).ToList();
                var norm = Normalize(professionName);
                var prof = professions.FirstOrDefault(p => !string.IsNullOrEmpty(p.Name) && Normalize(p.Name) == norm);
                if (prof != null && prof.Tiers != null && prof.Tiers.Count > 0)
                {
                    var tier1 = prof.Tiers[0];
                    TierSkills = tier1.Skills ?? new List<string>();
                    TierTalents = tier1.Talents ?? new List<string>();
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var draft = await GetOrCreateDraft();
            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();

            // ensure TierSkills/TierTalents are populated (POST doesn't run OnGet)
            var professionName = GetProfessionFromDraft(draft);
            if (!string.IsNullOrEmpty(professionName))
            {
                var professions = await LoadAllProfessions();
                var norm = Normalize(professionName);
                var prof = professions.FirstOrDefault(p => !string.IsNullOrEmpty(p.Name) && Normalize(p.Name) == norm);
                if (prof != null && prof.Tiers != null && prof.Tiers.Count > 0)
                {
                    var tier1 = prof.Tiers[0];
                    TierSkills = tier1.Skills ?? new List<string>();
                    TierTalents = tier1.Talents ?? new List<string>();
                }
            }

            // read points
            var pts = new Dictionary<string, int>();
            for (int i = 0; i < TierSkills.Count; i++)
            {
                var key = $"pts_{i}";
                if (Request.Form.ContainsKey(key) && int.TryParse(Request.Form[key], out var v))
                    pts[TierSkills[i]] = v;
                else
                    pts[TierSkills[i]] = 0;
            }

            var total = pts.Values.Sum(v => v);
            if (total != 40)
                return BadRequest("Musisz rozdzielić dokładnie 40 punktów.");

            foreach (var kv in pts)
                state[$"Advance_{TierSkills[pts.Keys.ToList().IndexOf(kv.Key)]}"] = kv.Value;

            // chosen talent
            var chosen = Request.Form.ContainsKey("chosenTalent") ? Request.Form["chosenTalent"].ToString() ?? string.Empty : string.Empty;
            if (!string.IsNullOrEmpty(chosen))
                state["ProfessionTier1Talent"] = chosen;

            draft.StateJson = JsonSerializer.Serialize(state);
            draft.Step = CharacterCreationStep.Equipment;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateEquipment");
        }

        private string? GetProfessionFromDraft(CharacterDraft draft)
        {
            try
            {
                var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
                if (state.TryGetValue("Profession", out var p))
                {
                    return p?.ToString();
                }
            }
            catch { }
            return null;
        }

        private async Task<List<Profession>> LoadAllProfessions()
        {
            var dir1 = Path.Combine(_env.ContentRootPath, "Content", "Professions");
            var dir2 = Path.Combine(_env.ContentRootPath, "Data", "Seed", "Content", "Professions");
            var files = new List<string>();
            if (Directory.Exists(dir1)) files.AddRange(Directory.GetFiles(dir1, "*.json"));
            if (Directory.Exists(dir2)) files.AddRange(Directory.GetFiles(dir2, "*.json"));
            var list = new List<Profession>();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            foreach (var f in files)
            {
                var txt = await System.IO.File.ReadAllTextAsync(f);
                var pList = JsonSerializer.Deserialize<List<Profession>>(txt, options);
                if (pList != null)
                    list.AddRange(pList);
            }
            return list;
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

        private async Task<CharacterDraft> GetOrCreateDraft()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.ProfessionSkillsAndTalents);
            if (draft != null)
                return draft;
            draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.ProfessionSkillsAndTalents, Experience = 0 };
            _db.CharacterDrafts.Add(draft);
            await _db.SaveChangesAsync();
            return draft;
        }
    }
}
