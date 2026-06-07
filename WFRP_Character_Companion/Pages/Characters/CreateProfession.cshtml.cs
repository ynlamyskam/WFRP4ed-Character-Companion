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

        public async Task<IActionResult> OnPostGeneratePoolAsync(string current)
        {
            var professions = await LoadAllProfessions();
            var pool = new List<Profession>();
            var currentP = professions.FirstOrDefault(p => p.Name == current) ?? professions[Random.Shared.Next(professions.Count)];
            pool.Add(currentP);
            var others = professions.Where(p => p.Name != currentP.Name).OrderBy(_ => Random.Shared.Next()).Take(2);
            pool.AddRange(others);
            Pool = pool;
            EligibleXp = 25;
            return Page();
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
            var dir = Path.Combine(_env.ContentRootPath, "Content", "Professions");
            var files = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.json") : Array.Empty<string>();
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
