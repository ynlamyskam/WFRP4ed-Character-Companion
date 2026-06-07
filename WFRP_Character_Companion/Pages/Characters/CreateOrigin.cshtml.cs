using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Services.CharacterCreation;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateOriginModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, CharacterDraftService draftService, CreationContentService content) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly CharacterDraftService _draftService = draftService;
        private readonly CreationContentService _content = content;

        public CharacterDraft Draft { get; set; } = default!;
        public Origin? RolledOrigin { get; set; }
        public List<Origin> FilteredOrigins { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            Draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);

            var allOrigins = await _content.LoadOriginsAsync();

            var matching = string.IsNullOrEmpty(Draft.Race)
                ? allOrigins
                : allOrigins.Where(o =>
                    !string.IsNullOrEmpty(o.Race) &&
                    CreationContentService.Normalize(o.Race) == CreationContentService.Normalize(Draft.Race)).ToList();

            FilteredOrigins = matching;
            RolledOrigin = matching.Count > 0 ? matching[Random.Shared.Next(matching.Count)] : null;

            return Page();
        }

        public async Task<IActionResult> OnPostAcceptAsync(string originName)
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            draft.Origin = originName;
            draft.OriginAccepted = true;
            draft.Experience += 10;
            draft.Step = CharacterCreationStep.PersonalInfo;
            await _db.SaveChangesAsync();
            return RedirectToPage("CreatePersonalInfo");
        }

        public async Task<IActionResult> OnPostChooseAsync(string originName)
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            draft.Origin = originName;
            draft.OriginAccepted = false;
            draft.Step = CharacterCreationStep.PersonalInfo;
            await _db.SaveChangesAsync();
            return RedirectToPage("CreatePersonalInfo");
        }
    }
}
