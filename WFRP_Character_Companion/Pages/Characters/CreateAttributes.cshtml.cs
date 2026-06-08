using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Helpers;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Services.CharacterCreation;
using System.Text.Json;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateAttributesModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, CharacterDraftService draftService, CreationContentService content) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly CharacterDraftService _draftService = draftService;
        private readonly CreationContentService _content = content;

        public List<string> AttributeOrder { get; set; } = Enum.GetNames(typeof(AttributeType)).ToList();
        public Dictionary<string, int> Bases { get; set; } = new();
        public List<int> Rolls { get; set; } = new();
        public string Stage { get; set; } = "roll";
        public List<string> AdvanceAttributes { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(bool advance = false)
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            await LoadRollStageAsync(draft);

            if (advance)
            {
                Stage = "advance";
                await LoadAdvanceAttributesAsync(draft);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAcceptRollAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);

            var rollsJson = TempData["Rolls"] as string;
            var basesJson = TempData["Bases"] as string;
            var persistedRolls = rollsJson != null ? JsonSerializer.Deserialize<List<int>>(rollsJson) ?? [] : [];
            var persistedBases = basesJson != null ? JsonSerializer.Deserialize<Dictionary<string, int>>(basesJson) ?? new Dictionary<string, int>() : new Dictionary<string, int>();

            if (persistedRolls.Count == 0)
            {
                await LoadRollStageAsync(draft);
                return this.PageWithError("Sesja wygasła — wylosuj cechy ponownie.");
            }

            var state = DraftStateHelper.Parse(draft.StateJson);
            for (int i = 0; i < AttributeOrder.Count; i++)
            {
                var baseVal = persistedBases.GetValueOrDefault(AttributeOrder[i], 20);
                var rollVal = i < persistedRolls.Count ? persistedRolls[i] : 0;
                DraftStateHelper.SetValue(state, $"Attr_{AttributeOrder[i]}", baseVal + rollVal);
            }

            draft.StateJson = DraftStateHelper.Serialize(state);
            draft.Experience += 50;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateAttributes", new { advance = true });
        }

        public async Task<IActionResult> OnPostRearrangeAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);

            Rolls = Enumerable.Range(0, AttributeOrder.Count).Select(_ => Roll2k10()).ToList();
            TempData["Rolls"] = JsonSerializer.Serialize(Rolls);
            await LoadBasesAsync(draft);
            TempData["Bases"] = JsonSerializer.Serialize(Bases);
            Stage = "rearrange";
            return Page();
        }

        public async Task<IActionResult> OnPostAcceptRearrangeAsync()
        {
            var rollsJson = TempData["Rolls"] as string;
            var rolls = rollsJson != null ? JsonSerializer.Deserialize<List<int>>(rollsJson) ?? [] : [];
            var basesJson = TempData["Bases"] as string;
            var persistedBases = basesJson != null ? JsonSerializer.Deserialize<Dictionary<string, int>>(basesJson) ?? new Dictionary<string, int>() : new Dictionary<string, int>();

            var assignedIndices = new List<int>();
            for (int i = 0; i < AttributeOrder.Count; i++)
            {
                var key = $"rollIndex_{i}";
                if (Request.Form.ContainsKey(key) && int.TryParse(Request.Form[key], out var idx))
                    assignedIndices.Add(idx);
            }

            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);

            if (assignedIndices.Count != assignedIndices.Distinct().Count())
            {
                Rolls = rolls;
                Bases = persistedBases;
                TempData["Rolls"] = rollsJson;
                TempData["Bases"] = basesJson;
                Stage = "rearrange";
                return this.PageWithError("Każdy rzut może być przypisany tylko do jednej cechy.");
            }

            var assigned = assignedIndices.Select(idx => rolls[idx]).ToList();
            var state = DraftStateHelper.Parse(draft.StateJson);
            for (int i = 0; i < AttributeOrder.Count; i++)
            {
                var baseVal = persistedBases.GetValueOrDefault(AttributeOrder[i], 20);
                var rollVal = i < assigned.Count ? assigned[i] : 0;
                DraftStateHelper.SetValue(state, $"Attr_{AttributeOrder[i]}", baseVal + rollVal);
            }

            draft.StateJson = DraftStateHelper.Serialize(state);
            draft.Experience += 25;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateAttributes", new { advance = true });
        }

        public IActionResult OnPostCancelRearrangeAsync() => RedirectToPage();

        public async Task<IActionResult> OnPostManualAllocateAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            await LoadBasesAsync(draft);
            Stage = "manual";
            TempData["Bases"] = JsonSerializer.Serialize(Bases);
            return Page();
        }

        public async Task<IActionResult> OnPostAcceptManualAsync()
        {
            var manual = new List<int>();
            for (int i = 0; i < AttributeOrder.Count; i++)
            {
                var key = $"manual_{i}";
                if (Request.Form.ContainsKey(key) && int.TryParse(Request.Form[key], out var v))
                    manual.Add(v);
                else
                    manual.Add(10);
            }

            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            var basesJson = TempData["Bases"] as string;
            var persistedBases = basesJson != null ? JsonSerializer.Deserialize<Dictionary<string, int>>(basesJson) ?? new Dictionary<string, int>() : new Dictionary<string, int>();

            if (manual.Any(m => m < 4 || m > 18))
            {
                Bases = persistedBases;
                Stage = "manual";
                TempData["Bases"] = basesJson;
                return this.PageWithError("Każda wartość musi być między 4 a 18.");
            }
            if (manual.Sum() != 100)
            {
                Bases = persistedBases;
                Stage = "manual";
                TempData["Bases"] = basesJson;
                return this.PageWithError($"Suma to {manual.Sum()} — wymagane dokładnie 100 punktów.");
            }

            var state = DraftStateHelper.Parse(draft.StateJson);
            for (int i = 0; i < AttributeOrder.Count; i++)
            {
                var baseVal = persistedBases.GetValueOrDefault(AttributeOrder[i], 20);
                DraftStateHelper.SetValue(state, $"Attr_{AttributeOrder[i]}", baseVal + manual[i]);
            }

            draft.StateJson = DraftStateHelper.Serialize(state);
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateAttributes", new { advance = true });
        }

        public async Task<IActionResult> OnPostAcceptAdvancesAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            var professions = await _content.LoadProfessionsAsync();
            var professionName = GetProfessionFromDraft(draft);
            var prof = !string.IsNullOrEmpty(professionName)
                ? _content.FindProfession(professions, professionName)
                : null;

            var advances = new Dictionary<string, int>();
            int total = 0;
            if (prof?.Tiers?.Count > 0)
            {
                var tier1 = prof.Tiers[0];
                AdvanceAttributes = tier1.Attributes ?? [];
                var attrs = tier1.Attributes ?? [];
                for (int i = 0; i < attrs.Count; i++)
                {
                    var attr = attrs[i];
                    var key = $"advance_{i}";
                    int v = 0;
                    if (Request.Form.ContainsKey(key) && int.TryParse(Request.Form[key], out var parsed))
                        v = parsed;
                    if (v < 0 || v > 5)
                    {
                        Stage = "advance";
                        return this.PageWithError("Każde rozwinięcie musi być między 0 a 5 punktów.");
                    }
                    advances[attr] = v;
                    total += v;
                }
            }

            if (prof?.Tiers?.Count > 0 && (prof.Tiers[0].Attributes?.Count ?? 0) > 0 && total != 5)
            {
                Stage = "advance";
                return this.PageWithError($"Rozdzielono {total} punktów — wymagane dokładnie 5.");
            }

            var state = DraftStateHelper.Parse(draft.StateJson);
            foreach (var kv in advances)
                DraftStateHelper.SetValue(state, $"Advance_{kv.Key}", kv.Value);

            draft.StateJson = DraftStateHelper.Serialize(state);
            draft.Step = CharacterCreationStep.StarSign;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateStarSign");
        }

        private async Task LoadRollStageAsync(CharacterDraft draft)
        {
            Rolls = Enumerable.Range(0, AttributeOrder.Count).Select(_ => Roll2k10()).ToList();
            TempData["Rolls"] = JsonSerializer.Serialize(Rolls);
            await LoadBasesAsync(draft);
            TempData["Bases"] = JsonSerializer.Serialize(Bases);
            Stage = "roll";
        }

        private async Task LoadBasesAsync(CharacterDraft draft)
        {
            var raceBases = await _content.LoadRacesAsync();
            var race = draft.Race ?? "Człowiek";
            var rb = raceBases.FirstOrDefault(r => string.Equals(r.Name, race, StringComparison.OrdinalIgnoreCase));
            if (rb == null)
                rb = new Race { Name = "Człowiek", Bases = AttributeOrder.ToDictionary(a => a, _ => 20) };
            foreach (var a in AttributeOrder)
                Bases[a] = rb.Bases.GetValueOrDefault(a, 20);
        }

        private async Task LoadAdvanceAttributesAsync(CharacterDraft draft)
        {
            var professions = await _content.LoadProfessionsAsync();
            var professionName = GetProfessionFromDraft(draft);
            var prof = !string.IsNullOrEmpty(professionName)
                ? _content.FindProfession(professions, professionName)
                : null;
            AdvanceAttributes = prof?.Tiers?.Count > 0 ? prof.Tiers[0].Attributes ?? [] : [];
        }

        private static string? GetProfessionFromDraft(CharacterDraft draft)
        {
            var state = DraftStateHelper.Parse(draft.StateJson);
            var name = DraftStateHelper.GetString(state, "Profession");
            return string.IsNullOrEmpty(name) ? null : name;
        }

        private static int Roll2k10() => Random.Shared.Next(1, 11) + Random.Shared.Next(1, 11);

        public static string AttributeLabel(string attributeName) =>
            Enum.TryParse<AttributeType>(attributeName, out var t) ? t.GetDisplayName() : attributeName;
    }
}
