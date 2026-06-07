using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateFateAndResillienceModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IWebHostEnvironment _env = env;

        public int FateBase { get; set; }
        public int FateToAssign { get; set; }
        public int HeroBase { get; set; }
        public int HeroToAssign { get; set; }

        public string Motivation { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var draft = await GetOrCreateDraft();
            var race = draft.Race ?? "Human";
            var raceBases = await LoadRaceBases();
            var rb = raceBases.FirstOrDefault(r => string.Equals(r.Name, race, StringComparison.OrdinalIgnoreCase)) ?? new RaceBase();

            FateBase = rb.FateBase;
            FateToAssign = rb.FateToAssign;
            HeroBase = rb.HeroBase;
            HeroToAssign = rb.HeroToAssign;

            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
            if (state.TryGetValue("Fate", out var f))
                FateBase = Convert.ToInt32(f);
            if (state.TryGetValue("Hero", out var h))
                HeroBase = Convert.ToInt32(h);
            if (state.TryGetValue("Motivation", out var m))
                Motivation = m?.ToString() ?? string.Empty;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.FateAndResillience);
            if (draft == null)
            {
                draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.FateAndResillience, Experience = 0 };
                _db.CharacterDrafts.Add(draft);
            }

            int fate = 0;
            int hero = 0;
            var mot = string.Empty;
            if (Request.Form.ContainsKey("FateAssign") && int.TryParse(Request.Form["FateAssign"], out var fv)) fate = fv;
            if (Request.Form.ContainsKey("HeroAssign") && int.TryParse(Request.Form["HeroAssign"], out var hv)) hero = hv;
            if (Request.Form.ContainsKey("Motivation")) mot = Request.Form["Motivation"].ToString() ?? string.Empty;

            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
            state["Fate"] = fate;
            state["Hero"] = hero;
            state["Motivation"] = mot;

            draft.StateJson = JsonSerializer.Serialize(state);
            draft.Step = CharacterCreationStep.RaceSkillsAndTalents;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateRaceSkillsAndTalents");
        }

        private async Task<List<RaceBase>> LoadRaceBases()
        {
            var path = Path.Combine(_env.ContentRootPath, "Content", "races.json");
            if (!System.IO.File.Exists(path))
                return new List<RaceBase>();

            var txt = await System.IO.File.ReadAllTextAsync(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<RaceBase>>(txt, options) ?? new List<RaceBase>();
        }

        private async Task<CharacterDraft> GetOrCreateDraft()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.FateAndResillience);
            if (draft != null)
                return draft;
            draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.FateAndResillience, Experience = 0 };
            _db.CharacterDrafts.Add(draft);
            await _db.SaveChangesAsync();
            return draft;
        }
    }
}
