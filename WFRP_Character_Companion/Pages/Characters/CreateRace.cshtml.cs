using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using Microsoft.EntityFrameworkCore;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateRaceModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public CreateRaceModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _env = env;
        }

        public CharacterDraft Draft { get; set; } = default!;
        public string RolledRace { get; set; } = string.Empty;
        public List<string> Races { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Draft = await GetOrCreateDraft();

            // load races from Content/races.json
            var path1 = Path.Combine(_env.ContentRootPath, "Content", "races.json");
            var path2 = Path.Combine(_env.ContentRootPath, "Data", "Seed", "Content", "races.json");
            string? txt = null;
            if (System.IO.File.Exists(path1))
                txt = await System.IO.File.ReadAllTextAsync(path1);
            else if (System.IO.File.Exists(path2))
                txt = await System.IO.File.ReadAllTextAsync(path2);

            if (!string.IsNullOrEmpty(txt))
            {
                try
                {
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<Race>>(txt, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                    Races = list.Select(r => r.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();
                }
                catch
                {
                    Races = new();
                }
            }

            if (Races.Any())
                RolledRace = Races[Random.Shared.Next(Races.Count)];

            return Page();
        }

        public async Task<IActionResult> OnPostAcceptAsync(string race)
        {
            var draft = await GetOrCreateDraft();

            draft.Race = race;
            draft.RaceAccepted = true;
            draft.Experience += 20;
            draft.Step = CharacterCreationStep.Background;

            await _db.SaveChangesAsync();

            return RedirectToPage("CreateOrigin");
        }

        public async Task<IActionResult> OnPostChooseAsync(string race)
        {
            var draft = await GetOrCreateDraft();

            draft.Race = race;
            draft.RaceAccepted = false;
            draft.Step = CharacterCreationStep.Background;

            await _db.SaveChangesAsync();

            return RedirectToPage("CreateOrigin");
        }

        private async Task<CharacterDraft> GetOrCreateDraft()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;

            var draft = await _db.CharacterDrafts
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Step == CharacterCreationStep.Race);

            if (draft != null)
                return draft;

            draft = new CharacterDraft
            {
                UserId = userId,
                Step = CharacterCreationStep.Race,
                Experience = 0
            };

            _db.CharacterDrafts.Add(draft);
            await _db.SaveChangesAsync();

            return draft;
        }

        //public string RollRace()
        //{
        //    return Race.All[Random.Shared.Next(Race.All.Count)];
        //}
    }
}
