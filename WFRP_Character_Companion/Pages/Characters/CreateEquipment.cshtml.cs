using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Services.CharacterCreation;
using System.Text.Json;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateEquipmentModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, CharacterDraftService draftService) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly CharacterDraftService _draftService = draftService;

        public List<string> Items { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
            if (state.TryGetValue("Items", out var items))
                Items = JsonSerializer.Deserialize<List<string>>(items.ToString() ?? "[]") ?? [];
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();

            if (Request.Form.ContainsKey("item") && !string.IsNullOrEmpty(Request.Form["item"]))
            {
                var item = Request.Form["item"].ToString() ?? string.Empty;
                var items = state.TryGetValue("Items", out var ex) ? JsonSerializer.Deserialize<List<string>>(ex.ToString() ?? "[]") ?? [] : [];
                items.Add(item);
                state["Items"] = items;
                draft.StateJson = JsonSerializer.Serialize(state);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostFinishAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            draft.Step = CharacterCreationStep.Details;
            await _db.SaveChangesAsync();
            return RedirectToPage("CreateDetails");
        }
    }
}
