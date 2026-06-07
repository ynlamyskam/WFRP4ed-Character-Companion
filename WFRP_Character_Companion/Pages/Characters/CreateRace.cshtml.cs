using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Services.CharacterCreation;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateRaceModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, CharacterDraftService draftService, CreationContentService content) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly CharacterDraftService _draftService = draftService;
        private readonly CreationContentService _content = content;

        public CharacterDraft Draft { get; set; } = default!;
        public string RolledRace { get; set; } = string.Empty;
        public List<string> Races { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            Draft = await _draftService.StartNewDraftAsync(user.Id);

            var list = await _content.LoadRacesAsync();
            Races = list.Select(r => r.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();

            if (Races.Any())
                RolledRace = Races[Random.Shared.Next(Races.Count)];

            return Page();
        }

        public async Task<IActionResult> OnPostAcceptAsync(string race)
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);

            draft.Race = race;
            draft.RaceAccepted = true;
            draft.Experience += 20;
            draft.Step = CharacterCreationStep.Background;

            await _db.SaveChangesAsync();
            return RedirectToPage("CreateOrigin");
        }

        public async Task<IActionResult> OnPostChooseAsync(string race)
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);

            draft.Race = race;
            draft.RaceAccepted = false;
            draft.Step = CharacterCreationStep.Background;

            await _db.SaveChangesAsync();
            return RedirectToPage("CreateOrigin");
        }
    }
}
