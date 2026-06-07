using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreatePersonalInfoModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        [BindProperty]
        public PersonalInfoInput Input { get; set; } = new();

        public CharacterDraft Draft { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Draft = await GetOrCreateDraft();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.PersonalInfo);
            if (draft == null)
            {
                draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.PersonalInfo, Experience = 0 };
                _db.CharacterDrafts.Add(draft);
            }

            // Save personal info into state json
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

        private async Task<CharacterDraft> GetOrCreateDraft()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.PersonalInfo);
            if (draft != null)
                return draft;
            draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.PersonalInfo, Experience = 0 };
            _db.CharacterDrafts.Add(draft);
            await _db.SaveChangesAsync();
            return draft;
        }
    }

    public class PersonalInfoInput
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public int Height { get; set; }
        public int Weight { get; set; }
        public string? EyeColor { get; set; }
        public string? HairColor { get; set; }
        public string? Description { get; set; }
    }
}
