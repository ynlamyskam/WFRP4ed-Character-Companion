using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateEquipmentModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        public List<string> Items { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var draft = await GetOrCreateDraft();
            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
            if (state.TryGetValue("Items", out var items))
            {
                Items = JsonSerializer.Deserialize<List<string>>(items.ToString() ?? "[]") ?? new();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var draft = await GetOrCreateDraft();
            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();

            if (Request.Form.ContainsKey("item") && !string.IsNullOrEmpty(Request.Form["item"]))
            {
                var item = Request.Form["item"].ToString() ?? string.Empty;
                var items = state.TryGetValue("Items", out var ex) ? JsonSerializer.Deserialize<List<string>>(ex.ToString() ?? "[]") ?? new() : new List<string>();
                items.Add(item);
                state["Items"] = items;

                draft.StateJson = JsonSerializer.Serialize(state);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostFinishAsync()
        {
            return RedirectToPage("CreateDetails");
        }

        private async Task<CharacterDraft> GetOrCreateDraft()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.Equipment);
            if (draft != null)
                return draft;
            draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.Equipment, Experience = 0 };
            _db.CharacterDrafts.Add(draft);
            await _db.SaveChangesAsync();
            return draft;
        }
    }
}
