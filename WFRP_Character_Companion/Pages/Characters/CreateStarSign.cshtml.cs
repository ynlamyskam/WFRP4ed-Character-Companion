using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateStarSignModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IWebHostEnvironment _env = env;

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
            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.StarSign);
            if (draft == null)
            {
                draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.StarSign, Experience = 0 };
                _db.CharacterDrafts.Add(draft);
            }

            var signs = await LoadStarSigns();
            string chosen = string.Empty;
            if (Request.Form.ContainsKey("chosenSign"))
                chosen = Request.Form["chosenSign"].ToString() ?? string.Empty;

            var selected = signs.FirstOrDefault(s => string.Equals(s.Name, chosen, StringComparison.OrdinalIgnoreCase))
                        ?? (signs.Any() ? signs[Random.Shared.Next(signs.Count)] : null);

            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
            state["StarSign"] = selected?.Name ?? string.Empty;
            draft.StateJson = JsonSerializer.Serialize(state);
            draft.Step = CharacterCreationStep.FateAndResillience;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateFateAndResillience");
        }

        private async Task<List<StarSign>> LoadStarSigns()
        {
            var path = Path.Combine(_env.ContentRootPath, "Content", "StarSigns.json");
            if (!System.IO.File.Exists(path))
                return new List<StarSign>();

            var txt = await System.IO.File.ReadAllTextAsync(path);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<StarSign>>(txt, opts) ?? new List<StarSign>();
        }
    }
}
