using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
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

            var raceBases = await _content.LoadRacesAsync();
            var race = draft.Race ?? "Człowiek";
            var rb = raceBases.FirstOrDefault(r => string.Equals(r.Name, race, StringComparison.OrdinalIgnoreCase));
            if (rb == null)
                rb = new Race { Name = "Człowiek", Bases = AttributeOrder.ToDictionary(a => a, _ => 20) };

            Rolls = Enumerable.Range(0, AttributeOrder.Count).Select(_ => Roll2k10()).ToList();
            TempData["Rolls"] = JsonSerializer.Serialize(Rolls);
            foreach (var a in AttributeOrder)
                Bases[a] = rb.Bases.ContainsKey(a) ? rb.Bases[a] : 20;
            TempData["Bases"] = JsonSerializer.Serialize(Bases);

            if (advance)
            {
                Stage = "advance";
                var professions = await _content.LoadProfessionsAsync();
                var professionName = GetProfessionFromDraft(draft);
                var prof = !string.IsNullOrEmpty(professionName)
                    ? _content.FindProfession(professions, professionName)
                    : null;

                AdvanceAttributes = prof?.Tiers?.Count > 0
                    ? prof.Tiers[0].Attributes ?? []
                    : [];
            }
            else
            {
                Stage = "roll";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAcceptRollAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);

            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();

            var rollsJson = TempData["Rolls"] as string;
            var basesJson = TempData["Bases"] as string;
            var persistedRolls = rollsJson != null ? JsonSerializer.Deserialize<List<int>>(rollsJson) ?? [] : [];
            var persistedBases = basesJson != null ? JsonSerializer.Deserialize<Dictionary<string, int>>(basesJson) ?? new Dictionary<string, int>() : new Dictionary<string, int>();

            for (int i = 0; i < AttributeOrder.Count; i++)
            {
                var baseVal = persistedBases.GetValueOrDefault(AttributeOrder[i], Bases.GetValueOrDefault(AttributeOrder[i], 20));
                var rollVal = i < persistedRolls.Count ? persistedRolls[i] : (i < Rolls.Count ? Rolls[i] : 0);
                state[$"Attr_{AttributeOrder[i]}"] = baseVal + rollVal;
            }

            draft.StateJson = JsonSerializer.Serialize(state);
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

            var raceBases = await _content.LoadRacesAsync();
            var race = draft.Race ?? "Człowiek";
            var rb = raceBases.FirstOrDefault(r => string.Equals(r.Name, race, StringComparison.OrdinalIgnoreCase));
            if (rb == null)
                rb = new Race { Name = "Człowiek", Bases = AttributeOrder.ToDictionary(a => a, _ => 20) };
            foreach (var a in AttributeOrder)
                Bases[a] = rb.Bases.ContainsKey(a) ? rb.Bases[a] : 20;
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
            if (assignedIndices.Count != assignedIndices.Distinct().Count())
                return BadRequest("Każdy rzut może być przypisany tylko do jednej cechy.");

            var assigned = assignedIndices.Select(idx => rolls[idx]).ToList();

            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);

            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
            for (int i = 0; i < AttributeOrder.Count; i++)
            {
                var baseVal = persistedBases.GetValueOrDefault(AttributeOrder[i], 20);
                var rollVal = i < assigned.Count ? assigned[i] : 0;
                state[$"Attr_{AttributeOrder[i]}"] = baseVal + rollVal;
            }

            draft.StateJson = JsonSerializer.Serialize(state);
            draft.Experience += 25;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateAttributes", new { advance = true });
        }

        public IActionResult OnPostCancelRearrangeAsync()
        {
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostManualAllocateAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);

            var raceBases = await _content.LoadRacesAsync();
            var race = draft.Race ?? "Człowiek";
            var rb = raceBases.FirstOrDefault(r => string.Equals(r.Name, race, StringComparison.OrdinalIgnoreCase));
            if (rb == null)
                rb = new Race { Name = "Człowiek", Bases = AttributeOrder.ToDictionary(a => a, _ => 20) };
            foreach (var a in AttributeOrder)
                Bases[a] = rb.Bases.ContainsKey(a) ? rb.Bases[a] : 20;
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

            if (manual.Any(m => m < 4 || m > 18))
                return BadRequest("Manual values must be between 4 and 18");
            if (manual.Sum() != 100)
                return BadRequest("Manual sum must be 100");

            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);

            var basesJson = TempData["Bases"] as string;
            var persistedBases = basesJson != null ? JsonSerializer.Deserialize<Dictionary<string, int>>(basesJson) ?? new Dictionary<string, int>() : new Dictionary<string, int>();

            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
            for (int i = 0; i < AttributeOrder.Count; i++)
            {
                var baseVal = persistedBases.GetValueOrDefault(AttributeOrder[i], 20);
                state[$"Attr_{AttributeOrder[i]}"] = baseVal + manual[i];
            }

            draft.StateJson = JsonSerializer.Serialize(state);
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateAttributes", new { advance = true });
        }

        public async Task<IActionResult> OnPostAcceptAdvancesAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);
            var professionName = GetProfessionFromDraft(draft);
            var professions = await _content.LoadProfessionsAsync();
            var prof = !string.IsNullOrEmpty(professionName)
                ? _content.FindProfession(professions, professionName)
                : null;

            var advances = new Dictionary<string, int>();
            int total = 0;
            if (prof?.Tiers?.Count > 0)
            {
                var tier1 = prof.Tiers[0];
                for (int i = 0; i < tier1.Attributes.Count; i++)
                {
                    var attr = tier1.Attributes[i];
                    var key = $"advance_{i}";
                    int v = 0;
                    if (Request.Form.ContainsKey(key) && int.TryParse(Request.Form[key], out var parsed))
                        v = parsed;
                    advances[attr] = v;
                    total += v;
                }
            }

            if (prof?.Tiers?.Count > 0 && prof.Tiers[0].Attributes.Count > 0 && total != 5)
                return BadRequest("Musisz rozdzielić dokładnie 5 punktów między rozwinięcia.");

            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
            foreach (var kv in advances)
                state[$"Advance_{kv.Key}"] = kv.Value;

            draft.StateJson = JsonSerializer.Serialize(state);
            draft.Step = CharacterCreationStep.StarSign;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateStarSign");
        }

        private string? GetProfessionFromDraft(CharacterDraft draft)
        {
            var state = DraftStateHelper.Parse(draft.StateJson);
            var name = DraftStateHelper.GetString(state, "Profession");
            return string.IsNullOrEmpty(name) ? null : name;
        }

        private static int Roll2k10() => Random.Shared.Next(1, 11) + Random.Shared.Next(1, 11);
    }
}
