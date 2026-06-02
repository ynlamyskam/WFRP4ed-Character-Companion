using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateOriginModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IWebHostEnvironment _env = env;

        public CharacterDraft Draft { get; set; } = default!;
        public Origin RolledOrigin { get; set; } = new();
        public List<Origin> AllOrigins { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Draft = await GetOrCreateDraft();

            // Load origins from file
            var path = Path.Combine(_env.ContentRootPath, "Data", "Seed", "Content", "origins.json");
            if (System.IO.File.Exists(path))
            {
                var json = await System.IO.File.ReadAllTextAsync(path);
                AllOrigins = JsonSerializer.Deserialize<List<Origin>>(json) ?? new();
            }

            // Filter by draft.Race when available
            var pool = string.IsNullOrEmpty(Draft.Race) ? AllOrigins : AllOrigins.Where(o => o.Race == Draft.Race).ToList();
            if (pool.Any())
                RolledOrigin = pool[Random.Shared.Next(pool.Count)];
            else if (AllOrigins.Any())
                RolledOrigin = AllOrigins[Random.Shared.Next(AllOrigins.Count)];

            return Page();
        }

        public async Task<IActionResult> OnPostAcceptAsync(string originName)
        {
            var draft = await GetOrCreateDraft();
            draft.Origin = originName;
            draft.OriginAccepted = true;
            draft.Experience += 10;
            draft.Step = CharacterCreationStep.PersonalInfo;
            await _db.SaveChangesAsync();
            return RedirectToPage("CreatePersonalInfo");
        }

        public async Task<IActionResult> OnPostChooseAsync(string originName)
        {
            var draft = await GetOrCreateDraft();
            draft.Origin = originName;
            draft.OriginAccepted = false;
            draft.Step = CharacterCreationStep.PersonalInfo;
            await _db.SaveChangesAsync();
            return RedirectToPage("CreatePersonalInfo");
        }

        private async Task<CharacterDraft> GetOrCreateDraft()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.Background);
            if (draft != null) 
                return draft;
            draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.Background, Experience = 0 };
            _db.CharacterDrafts.Add(draft);
            await _db.SaveChangesAsync();
            return draft;
        }
    }
}
