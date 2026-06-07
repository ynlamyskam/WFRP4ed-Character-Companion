using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Linq;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateAttributesModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IWebHostEnvironment _env = env;

        public List<string> AttributeOrder { get; set; } = Enum.GetNames(typeof(AttributeType)).ToList();
        public Dictionary<string, int> Bases { get; set; } = new();
        public List<int> Rolls { get; set; } = new();
        public string Stage { get; set; } = "roll"; 
        public List<string> AdvanceAttributes { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(bool advance = false)
        {
            var draft = await GetOrCreateDraft();

            
            var raceBases = await LoadRaceBases();
            var race = draft.Race ?? "Human";
            var rb = raceBases.FirstOrDefault(r => string.Equals(r.Name, race, StringComparison.OrdinalIgnoreCase));
            if (rb == null)
                rb = new Race { Name = "Human", Bases = AttributeOrder.ToDictionary(a => a, a => 20) };

            
            Rolls = Enumerable.Range(0, AttributeOrder.Count).Select(_ => Roll2k10()).ToList();
            
            TempData["Rolls"] = JsonSerializer.Serialize(Rolls);
            foreach (var a in AttributeOrder)
            {
                Bases[a] = rb.Bases.ContainsKey(a) ? rb.Bases[a] : 20;
            }

            TempData["Bases"] = JsonSerializer.Serialize(Bases);

            if (advance)
            {
                // show advance allocation stage
                Stage = "advance";
                // load profession Tier1 attributes
                var professions = await LoadAllProfessions();
                var professionName = GetProfessionFromDraft(draft);
                Profession? prof = null;
                if (!string.IsNullOrEmpty(professionName))
                {
                    var norm = Normalize(professionName);
                    prof = professions.FirstOrDefault(p => !string.IsNullOrEmpty(p.Name) && Normalize(p.Name) == norm);
                }

                if (prof == null)
                {
                    // fallback: try to match by draft.Race if profession not found
                    prof = professions.FirstOrDefault(p => string.Equals(p.Name, draft.Race, StringComparison.OrdinalIgnoreCase));
                }

                AdvanceAttributes = (prof != null && prof.Tiers != null && prof.Tiers.Count > 0) ? (prof.Tiers[0].Attributes ?? new List<string>()) : new List<string>();
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
            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.Attributes);
            if (draft == null)
            {
                draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.Attributes, Experience = 0 };
                _db.CharacterDrafts.Add(draft);
            }

            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();

            
            var rollsJson = TempData["Rolls"] as string;
            var basesJson = TempData["Bases"] as string;
            var persistedRolls = rollsJson != null ? JsonSerializer.Deserialize<List<int>>(rollsJson) ?? new() : new List<int>();
            var persistedBases = basesJson != null ? JsonSerializer.Deserialize<Dictionary<string,int>>(basesJson) ?? new() : new Dictionary<string,int>();

            
            for (int i = 0; i < AttributeOrder.Count; i++)
            {
                var baseVal = persistedBases.ContainsKey(AttributeOrder[i]) ? persistedBases[AttributeOrder[i]] : (Bases.ContainsKey(AttributeOrder[i]) ? Bases[AttributeOrder[i]] : 20);
                var rollVal = (i < persistedRolls.Count) ? persistedRolls[i] : (i < Rolls.Count ? Rolls[i] : 0);
                state[$"Attr_{AttributeOrder[i]}"] = baseVal + rollVal;
            }

            draft.StateJson = JsonSerializer.Serialize(state);
            draft.Experience += 50;
            draft.Step = CharacterCreationStep.StarSign; 
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateStarSign");
        }

        public async Task<IActionResult> OnPostRearrangeAsync()
        {
            
            var draft = await GetOrCreateDraft();
            
            Rolls = Enumerable.Range(0, AttributeOrder.Count).Select(_ => Roll2k10()).ToList();
            TempData["Rolls"] = JsonSerializer.Serialize(Rolls);
            
            var raceBases = await LoadRaceBases();
            var race = draft.Race ?? "Human";
            var rb = raceBases.FirstOrDefault(r => string.Equals(r.Name, race, StringComparison.OrdinalIgnoreCase));
            if (rb == null)
                rb = new Race { Name = "Human", Bases = AttributeOrder.ToDictionary(a => a, a => 20) };
            foreach (var a in AttributeOrder)
                Bases[a] = rb.Bases.ContainsKey(a) ? rb.Bases[a] : 20;
            TempData["Bases"] = JsonSerializer.Serialize(Bases);
            Stage = "rearrange";
            return Page();
        }

        public async Task<IActionResult> OnPostAcceptRearrangeAsync()
        {
            
            var rollsJson = TempData["Rolls"] as string;
            var rolls = rollsJson != null ? JsonSerializer.Deserialize<List<int>>(rollsJson) ?? new() : new List<int>();
            var basesJson = TempData["Bases"] as string;
            var persistedBases = basesJson != null ? JsonSerializer.Deserialize<Dictionary<string,int>>(basesJson) ?? new() : new Dictionary<string,int>();
            var assigned = new List<int>();
            for (int i = 0; i < AttributeOrder.Count; i++)
            {
                var key = $"rollIndex_{i}";
                if (Request.Form.ContainsKey(key))
                {
                    if (int.TryParse(Request.Form[key], out var idx) && idx >= 0 && idx < rolls.Count)
                        assigned.Add(rolls[idx]);
                }
            }

            // validate that same roll isn't assigned multiple times by checking indices uniqueness
            var assignedIndices = new List<int>();
            for (int i = 0; i < AttributeOrder.Count; i++)
            {
                var key = $"rollIndex_{i}";
                if (Request.Form.ContainsKey(key) && int.TryParse(Request.Form[key], out var idx))
                    assignedIndices.Add(idx);
            }
            if (assignedIndices.Count != assignedIndices.Distinct().Count())
                return BadRequest("Każdy rzut może być przypisany tylko do jednej cechy.");

           
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.Attributes);
            if (draft == null)
            {
                draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.Attributes, Experience = 0 };
                _db.CharacterDrafts.Add(draft);
            }

            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
            for (int i = 0; i < AttributeOrder.Count; i++)
            {
                var baseVal = persistedBases.ContainsKey(AttributeOrder[i]) ? persistedBases[AttributeOrder[i]] : (Bases.ContainsKey(AttributeOrder[i]) ? Bases[AttributeOrder[i]] : 20);
                var rollVal = (i < assigned.Count) ? assigned[i] : (i < rolls.Count ? rolls[i] : 0);
                var val = baseVal + rollVal;
                state[$"Attr_{AttributeOrder[i]}"] = val;
            }

            draft.StateJson = JsonSerializer.Serialize(state);
            draft.Experience += 25;
            draft.Step = CharacterCreationStep.Attributes;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateAttributes", new { advance = true });
        }

        public async Task<IActionResult> OnPostCancelRearrangeAsync()
        {
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostManualAllocateAsync()
        {
            
            var draft = await GetOrCreateDraft();
            
            var raceBases = await LoadRaceBases();
            var race = draft.Race ?? "Human";
            var rb = raceBases.FirstOrDefault(r => string.Equals(r.Name, race, StringComparison.OrdinalIgnoreCase));
            if (rb == null)
                rb = new Race { Name = "Human", Bases = AttributeOrder.ToDictionary(a => a, a => 20) };
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
            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.Attributes);
            if (draft == null)
            {
                draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.Attributes, Experience = 0 };
                _db.CharacterDrafts.Add(draft);
            }


            var basesJson = TempData["Bases"] as string;
            var persistedBases = basesJson != null ? JsonSerializer.Deserialize<Dictionary<string,int>>(basesJson) ?? new() : new Dictionary<string,int>();

            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
            for (int i = 0; i < AttributeOrder.Count; i++)
            {
                var baseVal = persistedBases.ContainsKey(AttributeOrder[i]) ? persistedBases[AttributeOrder[i]] : (Bases.ContainsKey(AttributeOrder[i]) ? Bases[AttributeOrder[i]] : 20);
                state[$"Attr_{AttributeOrder[i]}"] = baseVal + manual[i];
            }

            draft.StateJson = JsonSerializer.Serialize(state);
            draft.Step = CharacterCreationStep.Attributes;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateAttributes", new { advance = true });
        }

        public async Task<IActionResult> OnPostAcceptAdvancesAsync()
        {
            // read advance allocations
            var draft = await GetOrCreateDraft();
            var professionName = GetProfessionFromDraft(draft);
            var professions = await LoadAllProfessions();
            var prof = professions.FirstOrDefault(p => p.Name == professionName);

            var advances = new Dictionary<string, int>();
            int total = 0;
            if (prof != null && prof.Tiers != null && prof.Tiers.Count > 0)
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

            if (total != 5)
                return BadRequest("Musisz rozdzielić dokładnie 5 punktów między rozwinięcia.");

            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
            foreach (var kv in advances)
            {
                state[$"Advance_{kv.Key}"] = kv.Value;
            }

            draft.StateJson = JsonSerializer.Serialize(state);
            draft.Step = CharacterCreationStep.StarSign;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateStarSign");
        }

        private string? GetProfessionFromDraft(CharacterDraft draft)
        {
            try
            {
                var state = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.StateJson) ?? new Dictionary<string, object>();
                if (state.TryGetValue("Profession", out var p))
                    return p?.ToString();
            }
            catch { }
            return null;
        }

        private async Task<List<Profession>> LoadAllProfessions()
        {
            var dir1 = Path.Combine(_env.ContentRootPath, "Content", "Professions");
            var dir2 = Path.Combine(_env.ContentRootPath, "Data", "Seed", "Content", "Professions");
            var files = new List<string>();
            if (Directory.Exists(dir1)) files.AddRange(Directory.GetFiles(dir1, "*.json"));
            if (Directory.Exists(dir2)) files.AddRange(Directory.GetFiles(dir2, "*.json"));
            var list = new List<Profession>();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            foreach (var f in files)
            {
                var txt = await System.IO.File.ReadAllTextAsync(f);
                var pList = JsonSerializer.Deserialize<List<Profession>>(txt, options);
                if (pList != null)
                    list.AddRange(pList);
            }
            return list;
        }

        private int Roll2k10()
        {
            return Random.Shared.Next(1, 11) + Random.Shared.Next(1, 11);
        }

        private async Task<List<Race>> LoadRaceBases()
        {
            var path1 = Path.Combine(_env.ContentRootPath, "Content", "races.json");
            var path2 = Path.Combine(_env.ContentRootPath, "Data", "Seed", "Content", "races.json");
            string? txt = null;
            if (System.IO.File.Exists(path1))
                txt = await System.IO.File.ReadAllTextAsync(path1);
            else if (System.IO.File.Exists(path2))
                txt = await System.IO.File.ReadAllTextAsync(path2);

            if (string.IsNullOrEmpty(txt))
                return new List<Race>();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Race>>(txt, options) ?? new List<Race>();
        }

        private async Task<CharacterDraft> GetOrCreateDraft()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.Attributes);
            if (draft != null)
                return draft;
            draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.Attributes, Experience = 0 };
            _db.CharacterDrafts.Add(draft);
            await _db.SaveChangesAsync();
            return draft;
        }

        private static string Normalize(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var form = s.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in form)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant().Replace(" ", "");
        }
    }
}
