using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Services.CharacterCreation;
using System.Text.Json;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateProfessionSkillsAndTalentsModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, CharacterDraftService draftService, CreationContentService content) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly CharacterDraftService _draftService = draftService;
        private readonly CreationContentService _content = content;

        public List<string> TierSkills { get; set; } = new();
        public List<string> TierTalents { get; set; } = new();
        public string FoundProfessionName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            await LoadProfessionTierData(draft);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            await LoadProfessionTierData(draft);

            var state = DraftStateHelper.Parse(draft.StateJson);

            var pts = new Dictionary<string, int>();
            for (int i = 0; i < TierSkills.Count; i++)
            {
                var key = $"pts_{i}";
                if (Request.Form.ContainsKey(key) && int.TryParse(Request.Form[key], out var v))
                    pts[TierSkills[i]] = v;
                else
                    pts[TierSkills[i]] = 0;
            }

            if (pts.Values.Sum() != 40)
                return BadRequest("Musisz rozdzielić dokładnie 40 punktów.");

            foreach (var kv in pts)
            {
                var existingKey = $"Advance_{kv.Key}";
                var existing = DraftStateHelper.GetInt(state, existingKey);
                DraftStateHelper.SetValue(state, existingKey, existing + kv.Value);
            }

            var chosen = Request.Form.ContainsKey("chosenTalent") ? Request.Form["chosenTalent"].ToString() ?? string.Empty : string.Empty;
            if (!string.IsNullOrEmpty(chosen))
                DraftStateHelper.SetValue(state, "ProfessionTier1Talent", chosen);

            draft.StateJson = DraftStateHelper.Serialize(state);
            draft.Step = CharacterCreationStep.Equipment;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateEquipment");
        }

        private async Task LoadProfessionTierData(CharacterDraft draft)
        {
            var professionName = GetProfessionFromDraft(draft);
            FoundProfessionName = professionName ?? string.Empty;
            if (string.IsNullOrEmpty(professionName))
                return;

            var professions = await _content.LoadProfessionsAsync();
            var prof = _content.FindProfession(professions, professionName);
            if (prof?.Tiers?.Count > 0)
            {
                var tier1 = prof.Tiers[0];
                TierSkills = tier1.Skills ?? [];
                TierTalents = tier1.Talents ?? [];
            }
        }

        private static string? GetProfessionFromDraft(CharacterDraft draft)
        {
            var state = DraftStateHelper.Parse(draft.StateJson);
            var name = DraftStateHelper.GetString(state, "Profession");
            return string.IsNullOrEmpty(name) ? null : name;
        }
    }
}
