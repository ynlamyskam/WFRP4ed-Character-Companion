using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Services.CharacterCreation;
using System.Text.Json;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateProfessionModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, CharacterDraftService draftService, CreationContentService content) : PageModel
    {
        private const string PoolKeysTempKey = "ProfessionPoolKeys";

        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly CharacterDraftService _draftService = draftService;
        private readonly CreationContentService _content = content;

        public Profession? Rolled { get; set; }
        public List<Profession> Pool { get; set; } = new();
        public int EligibleXp { get; set; } = 50;
        public int TotalProfessionsLoaded { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            await _draftService.GetOrCreateActiveDraftAsync(user.Id);

            var all = await _content.LoadProfessionsAsync();
            TotalProfessionsLoaded = all.Count;

            if (TempData[PoolKeysTempKey] is string poolJson)
            {
                var keys = JsonSerializer.Deserialize<List<string>>(poolJson) ?? [];
                Pool = keys
                    .Select(k => _content.FindProfession(all, k))
                    .Where(p => p != null)
                    .Cast<Profession>()
                    .ToList();
                EligibleXp = TempData["ProfessionPoolXp"] switch
                {
                    int xp => xp,
                    string s when int.TryParse(s, out var xp) => xp,
                    _ => 25
                };
                return Page();
            }

            if (all.Count > 0)
                Rolled = all[Random.Shared.Next(all.Count)];
            EligibleXp = 50;
            return Page();
        }

        public async Task<IActionResult> OnPostAcceptAsync(string chosen, int eligibleXp)
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);

            var state = DraftStateHelper.Parse(draft.StateJson);
            DraftStateHelper.SetValue(state, "Profession", chosen);
            draft.StateJson = DraftStateHelper.Serialize(state);
            draft.Experience += eligibleXp;
            draft.Step = CharacterCreationStep.Attributes;

            await _db.SaveChangesAsync();
            return RedirectToPage("CreateAttributes");
        }

        public async Task<IActionResult> OnPostGeneratePoolAsync(string current)
        {
            var professions = await _content.LoadProfessionsAsync();
            if (professions.Count == 0)
                return RedirectToPage();

            var currentP = _content.FindProfession(professions, current)
                ?? professions[Random.Shared.Next(professions.Count)];

            var currentKey = CreationContentService.ToKey(currentP);
            var others = professions
                .Where(p => CreationContentService.ToKey(p) != currentKey)
                .OrderBy(_ => Random.Shared.Next())
                .Take(2)
                .ToList();

            var pool = new List<Profession> { currentP };
            pool.AddRange(others);

            pool = pool
                .GroupBy(p => CreationContentService.ToKey(p))
                .Select(g => g.First())
                .Take(3)
                .ToList();

            TempData[PoolKeysTempKey] = JsonSerializer.Serialize(pool.Select(CreationContentService.ToKey).ToList());
            TempData["ProfessionPoolXp"] = 25;
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChooseFromPoolAsync(string chosen)
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);

            var state = DraftStateHelper.Parse(draft.StateJson);
            DraftStateHelper.SetValue(state, "Profession", chosen);
            draft.StateJson = DraftStateHelper.Serialize(state);
            draft.Experience += 25;
            draft.Step = CharacterCreationStep.Attributes;

            await _db.SaveChangesAsync();
            return RedirectToPage("CreateAttributes");
        }

        public async Task<IActionResult> OnPostRerollAsync()
        {
            TempData.Remove(PoolKeysTempKey);
            TempData.Remove("ProfessionPoolXp");
            return RedirectToPage();
        }
    }
}
