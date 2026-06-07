using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateOriginModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IWebHostEnvironment _env = env;

        public CharacterDraft Draft { get; set; } = default!;
        public Origin? RolledOrigin { get; set; }
        public List<Origin> AllOrigins { get; set; } = new();
        public List<Origin> FilteredOrigins { get; set; } = new();
        public string DebugDraftJson { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            Draft = await GetOrCreateDraft();

            // if race not set on this draft (possible if a new draft was created), try to find latest draft for the user
            if (string.IsNullOrEmpty(Draft.Race))
            {
                var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
                var latest = await _db.CharacterDrafts.Where(x => x.UserId == user.Id).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
                if (latest != null && !string.IsNullOrEmpty(latest.Race))
                {
                    Draft = latest;
                }
            }

            // Load origins from Data/Seed/Content/origins*.json or Content/origins*.json
            var contentDir1 = Path.Combine(_env.ContentRootPath, "Content");
            var contentDir2 = Path.Combine(_env.ContentRootPath, "Data", "Seed", "Content");
            var originFiles = new List<string>();
            if (Directory.Exists(contentDir1))
                originFiles.AddRange(Directory.GetFiles(contentDir1, "origins*.json", SearchOption.TopDirectoryOnly));
            if (Directory.Exists(contentDir2))
                originFiles.AddRange(Directory.GetFiles(contentDir2, "origins*.json", SearchOption.TopDirectoryOnly));

            var combined = new List<Origin>();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            foreach (var f in originFiles.OrderBy(f => f))
            {
                var json = await System.IO.File.ReadAllTextAsync(f);
                var list = JsonSerializer.Deserialize<List<Origin>>(json, options);
                if (list != null)
                    combined.AddRange(list);
            }

            AllOrigins = combined;
            DebugDraftJson = Draft.StateJson;

            // Filter by draft.Race when available (match by race name)
            var matching = new List<Origin>();
            if (string.IsNullOrEmpty(Draft.Race))
            {
                matching = AllOrigins;
            }
            else
            {
                string normDraft = Normalize(Draft.Race);
                matching = AllOrigins.Where(o => !string.IsNullOrEmpty(o.Race) && Normalize(o.Race) == normDraft).ToList();
                if (!matching.Any())
                {
                    // try contains match
                    matching = AllOrigins.Where(o => !string.IsNullOrEmpty(o.Race) && Normalize(o.Race).Contains(normDraft)).ToList();
                }
            }

            FilteredOrigins = matching;
            if (matching.Any())
                RolledOrigin = matching[Random.Shared.Next(matching.Count)];
            else
                RolledOrigin = null;

            return Page();
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

        public async Task<IActionResult> OnPostAcceptAsync(string originName)
        {
            var draft = await GetOrCreateDraft();
            draft.Origin = originName;
            draft.OriginAccepted = true;
            draft.Experience += 10;
            draft.Step = CharacterCreationStep.PersonalInfo;
            await _db.SaveChangesAsync();
            return RedirectToPage("CreatePersonalInfo");
        }

        public async Task<IActionResult> OnPostChooseAsync(string originName)
        {
            var draft = await GetOrCreateDraft();
            draft.Origin = originName;
            draft.OriginAccepted = false;
            draft.Step = CharacterCreationStep.PersonalInfo;
            await _db.SaveChangesAsync();
            return RedirectToPage("CreatePersonalInfo");
        }

        private async Task<CharacterDraft> GetOrCreateDraft()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _db.CharacterDrafts.FirstOrDefaultAsync(x => x.UserId == user.Id && x.Step == CharacterCreationStep.Background);
            if (draft != null) 
                return draft;
            draft = new CharacterDraft { UserId = user.Id, Step = CharacterCreationStep.Background, Experience = 0 };
            _db.CharacterDrafts.Add(draft);
            await _db.SaveChangesAsync();
            return draft;
        }
    }
}
