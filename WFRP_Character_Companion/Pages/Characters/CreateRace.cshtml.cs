using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using Microsoft.EntityFrameworkCore;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateRaceModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : PageModel
    {
        private readonly ApplicationDbContext _db = db;

        private readonly UserManager<ApplicationUser> _userManager;

        public CharacterDraft Draft { get; set; } = default!;
        public string RolledRace { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            Draft = await GetOrCreateDraft();

            //RolledRace = RollRace();

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
