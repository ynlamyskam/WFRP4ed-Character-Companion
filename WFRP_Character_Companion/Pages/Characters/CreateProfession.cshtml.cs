using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateProfessionModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IWebHostEnvironment _env = env;

        public Profession? Rolled { get; set; }
        public List<Profession> Pool { get; set; } = new();
        public int EligibleXp { get; set; } = 50;

        public async Task<IActionResult> OnGetAsync()
        {
            var draft = await GetOrCreateDraft();
            var professions = await LoadAllProfessions();
            if (professions.Count > 0)
                Rolled = professions[Random.Shared.Next(professions.Count)];
            EligibleXp = 50;
            return Page();
        }

        public async Task<IActionResult> OnPostAcceptAsync(string chosen, int eligibleXp)
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.Profession);
            if (draft == null)
            {
                draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.Profession, Experience = 0 };
                _db.CharacterDrafts.Add(draft);
            }

            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
            state["Profession"] = chosen;
            draft.StateJson = JsonSerializer.Serialize(state);
            draft.Experience += eligibleXp;
            draft.Step = CharacterCreationStep.Attributes;

            await _db.SaveChangesAsync();
            return RedirectToPage("CreateAttributes");
        }

        public async Task<IActionResult> OnPostGeneratePoolAsync(string current, int eligibleXp)
        {
            var professions = await LoadAllProfessions();
            var pool = new List<Profession>();
            // If eligibleXp is 0 (this was a reroll without XP), do not provide a selectable pool
            if (eligibleXp == 0)
            {
                // just reroll single profession without pool
                Rolled = professions.Count > 0 ? professions[Random.Shared.Next(professions.Count)] : null;
                EligibleXp = 0;
                Pool = new();
                return Page();
            }

            var currentP = FindProfession(professions, current) ?? (professions.Count > 0 ? professions[Random.Shared.Next(professions.Count)] : null);
            if (currentP != null) pool.Add(currentP);

            var others = professions.Where(p => currentP == null || p.Name != currentP.Name)
                                    .OrderBy(_ => Random.Shared.Next())
                                    .ToList();

            foreach (var o in others)
            {
                if (pool.Count >= 3) break;
                if (pool.All(p => p.Name != o.Name)) pool.Add(o);
            }

            // ensure pool size at most 3 and distinct
            Pool = pool.GroupBy(p => p.Name).Select(g => g.First()).Take(3).ToList();
            EligibleXp = 25;
            return Page();
        }

        private Profession? FindProfession(List<Profession> professions, string? name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var exact = professions.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;
            var norm = Normalize(name);
            var normMatch = professions.FirstOrDefault(p => !string.IsNullOrEmpty(p.Name) && Normalize(p.Name) == norm);
            if (normMatch != null) return normMatch;
            var contains = professions.FirstOrDefault(p => !string.IsNullOrEmpty(p.Name) && Normalize(p.Name).Contains(norm));
            return contains;
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

        public async Task<IActionResult> OnPostChooseFromPoolAsync(string chosen)
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.Profession);
            if (draft == null)
            {
                draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.Profession, Experience = 0 };
                _db.CharacterDrafts.Add(draft);
            }

            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
            state["Profession"] = chosen;
            draft.StateJson = JsonSerializer.Serialize(state);
            draft.Experience += 25;
            draft.Step = CharacterCreationStep.Attributes;

            await _db.SaveChangesAsync();
            return RedirectToPage("CreateAttributes");
        }

        public async Task<IActionResult> OnPostRerollAsync()
        {
            var professions = await LoadAllProfessions();
            Rolled = professions[Random.Shared.Next(professions.Count)];
            EligibleXp = 0;
            return Page();
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

        private async Task<CharacterDraft> GetOrCreateDraft()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.Profession);
            if (draft != null)
                return draft;
            draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.Profession, Experience = 0 };
            _db.CharacterDrafts.Add(draft);
            await _db.SaveChangesAsync();
            return draft;
        }
    }

    public class Profession
    {
        public string? Class { get; set; }
        public string? Name { get; set; }
        public List<ProfessionTier> Tiers { get; set; } = new();
    }

    public class ProfessionTier
    {
        public string? Status { get; set; }
        public List<string> Attributes { get; set; } = new();
        public List<string> Skills { get; set; } = new();
        public List<string> Talents { get; set; } = new();
    }
}
