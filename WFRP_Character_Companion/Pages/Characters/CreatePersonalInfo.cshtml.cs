using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Services.CharacterCreation;
using System.Text.Json;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreatePersonalInfoModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, CharacterDraftService draftService) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly CharacterDraftService _draftService = draftService;

        [BindProperty]
        public PersonalInfoInput Input { get; set; } = new();

        public CharacterDraft Draft { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            Draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);

            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
            state["Name"] = Input.Name ?? string.Empty;
            state["Age"] = Input.Age;
            state["Height"] = Input.Height;
            state["Weight"] = Input.Weight;
            state["EyeColor"] = Input.EyeColor ?? string.Empty;
            state["HairColor"] = Input.HairColor ?? string.Empty;
            state["Description"] = Input.Description ?? string.Empty;

            draft.StateJson = JsonSerializer.Serialize(state);
            draft.Step = CharacterCreationStep.Profession;

            await _db.SaveChangesAsync();

            return RedirectToPage("CreateProfession");
        }
    }

    public class PersonalInfoInput
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public int Height { get; set; }
        public int Weight { get; set; }
        public string? EyeColor { get; set; }
        public string? HairColor { get; set; }
        public string? Description { get; set; }
    }
}
