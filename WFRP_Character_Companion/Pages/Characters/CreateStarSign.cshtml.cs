using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Services.CharacterCreation;
using System.Text.Json;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateStarSignModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env, CharacterDraftService draftService) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IWebHostEnvironment _env = env;
        private readonly CharacterDraftService _draftService = draftService;

        public StarSign? Rolled { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var signs = await LoadStarSigns();
            if (signs.Any())
                Rolled = signs[Random.Shared.Next(signs.Count)];
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);

            var signs = await LoadStarSigns();
            string chosen = string.Empty;
            if (Request.Form.ContainsKey("chosenSign"))
                chosen = Request.Form["chosenSign"].ToString() ?? string.Empty;

            var selected = signs.FirstOrDefault(s => string.Equals(s.Name, chosen, StringComparison.OrdinalIgnoreCase))
                        ?? (signs.Count > 0 ? signs[Random.Shared.Next(signs.Count)] : null);

            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
            state["StarSign"] = selected?.Name ?? string.Empty;
            draft.StateJson = JsonSerializer.Serialize(state);
            draft.Step = CharacterCreationStep.FateAndResillience;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateFateAndResillience");
        }

        private async Task<List<StarSign>> LoadStarSigns()
        {
            var path1 = Path.Combine(_env.ContentRootPath, "Content", "StarSigns.json");
            var path2 = Path.Combine(_env.ContentRootPath, "Data", "Seed", "Content", "StarSigns.json");
            string? txt = null;
            if (System.IO.File.Exists(path1)) txt = await System.IO.File.ReadAllTextAsync(path1);
            else if (System.IO.File.Exists(path2)) txt = await System.IO.File.ReadAllTextAsync(path2);
            if (string.IsNullOrEmpty(txt)) return [];
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<StarSign>>(txt, opts) ?? [];
        }
    }
}
