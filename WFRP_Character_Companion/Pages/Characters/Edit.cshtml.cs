using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Helpers;
using WFRP_Character_Companion.Models;

namespace WFRP_Character_Companion.Pages.Characters
{
    [Authorize]
    public class EditModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        public Character Character { get; set; } = default!;
        public List<string> Items { get; set; } = [];
        public string NewItem { get; set; } = string.Empty;
        public int MaxEncumbrance { get; set; }

        [BindProperty]
        public EditInput Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var character = await LoadOwnedCharacter(id);
            if (character == null) return NotFound();

            Character = character;
            MaxEncumbrance = CharacterRulesHelper.GetMaxEncumbrance(character);
            Input = new EditInput
            {
                Name = character.Name,
                Age = character.Age,
                Height = character.Height,
                Weight = character.Weight,
                EyeColor = character.EyeColor,
                HairColor = character.HairColor,
                Description = character.Description
            };
            Items = DeserializeItems(character.ItemsJson);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var character = await LoadOwnedCharacter(id);
            if (character == null) return NotFound();

            character.Name = Input.Name?.Trim() ?? character.Name;
            character.Age = Input.Age;
            character.Height = Input.Height;
            character.Weight = Input.Weight;
            character.EyeColor = Input.EyeColor ?? string.Empty;
            character.HairColor = Input.HairColor ?? string.Empty;
            character.Description = Input.Description ?? string.Empty;

            await _db.SaveChangesAsync();
            return RedirectToPage("/Characters/Details", new { id });
        }

        public async Task<IActionResult> OnPostAddItemAsync(int id, string newItem)
        {
            var character = await LoadOwnedCharacter(id);
            if (character == null) return NotFound();

            var items = DeserializeItems(character.ItemsJson);
            if (!string.IsNullOrWhiteSpace(newItem))
            {
                items.Add(newItem.Trim());
                character.ItemsJson = JsonSerializer.Serialize(items);
                await _db.SaveChangesAsync();
            }
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostRemoveItemAsync(int id, int index)
        {
            var character = await LoadOwnedCharacter(id);
            if (character == null) return NotFound();

            var items = DeserializeItems(character.ItemsJson);
            if (index >= 0 && index < items.Count)
            {
                items.RemoveAt(index);
                character.ItemsJson = JsonSerializer.Serialize(items);
                await _db.SaveChangesAsync();
            }
            return RedirectToPage(new { id });
        }

        private async Task<Character?> LoadOwnedCharacter(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            return await _db.Characters
                .Include(c => c.Attributes)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);
        }

        private static List<string> DeserializeItems(string json)
        {
            try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
            catch { return []; }
        }
    }

    public class EditInput
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
