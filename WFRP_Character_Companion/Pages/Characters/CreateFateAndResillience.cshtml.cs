using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Services.CharacterCreation;

namespace WFRP_Character_Companion.Pages.Characters
{
    public class CreateFateAndResillienceModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, CharacterDraftService draftService, CreationContentService content) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly CharacterDraftService _draftService = draftService;
        private readonly CreationContentService _content = content;

        /// <summary>Bazowe PP z rasy (minFate).</summary>
        public int FateBase { get; set; }
        /// <summary>Bazowe PB z rasy (minResilience).</summary>
        public int ResilienceBase { get; set; }
        /// <summary>Ile punktów można rozdzielić między PP i PB (pointsToSpend).</summary>
        public int PointsToSpend { get; set; }
        /// <summary>Już przypisane dodatkowe PP (z formularza).</summary>
        public int FateBonus { get; set; }
        /// <summary>Już przypisane dodatkowe PB (z formularza).</summary>
        public int ResilienceBonus { get; set; }
        public string Motivation { get; set; } = string.Empty;
        public string RaceName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);

            RaceName = draft.Race ?? "Człowiek";
            var raceBases = await _content.LoadRacesAsync();
            var rb = raceBases.FirstOrDefault(r => string.Equals(r.Name, RaceName, StringComparison.OrdinalIgnoreCase));
            if (rb != null)
            {
                FateBase = rb.MinFate;
                ResilienceBase = rb.MinResilience;
                PointsToSpend = rb.PointsToSpend;
            }

            var state = DraftStateHelper.Parse(draft.StateJson);
            FateBonus = DraftStateHelper.GetInt(state, "FateBonus");
            ResilienceBonus = DraftStateHelper.GetInt(state, "ResilienceBonus");
            Motivation = DraftStateHelper.GetString(state, "Motivation");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException();
            var draft = await _draftService.GetOrCreateActiveDraftAsync(user.Id);

            int fateBonus = 0;
            int resilienceBonus = 0;
            var mot = string.Empty;
            if (Request.Form.ContainsKey("FateBonus") && int.TryParse(Request.Form["FateBonus"], out var fv)) fateBonus = fv;
            if (Request.Form.ContainsKey("ResilienceBonus") && int.TryParse(Request.Form["ResilienceBonus"], out var rv)) resilienceBonus = rv;
            if (Request.Form.ContainsKey("Motivation")) mot = Request.Form["Motivation"].ToString() ?? string.Empty;

            var raceName = draft.Race ?? "Człowiek";
            var rb = (await _content.LoadRacesAsync())
                .FirstOrDefault(r => string.Equals(r.Name, raceName, StringComparison.OrdinalIgnoreCase));
            var pool = rb?.PointsToSpend ?? 0;

            if (fateBonus < 0 || resilienceBonus < 0 || fateBonus + resilienceBonus != pool)
                return BadRequest($"Musisz rozdzielić dokładnie {pool} punktów między Przeznaczenie i Bohaterstwo.");

            var state = DraftStateHelper.Parse(draft.StateJson);
            DraftStateHelper.SetValue(state, "FateBonus", fateBonus);
            DraftStateHelper.SetValue(state, "ResilienceBonus", resilienceBonus);
            DraftStateHelper.SetValue(state, "Fate", (rb?.MinFate ?? 0) + fateBonus);
            DraftStateHelper.SetValue(state, "Resilience", (rb?.MinResilience ?? 0) + resilienceBonus);
            DraftStateHelper.SetValue(state, "Motivation", mot);

            draft.StateJson = DraftStateHelper.Serialize(state);
            draft.Step = CharacterCreationStep.RaceSkillsAndTalents;
            await _db.SaveChangesAsync();

            return RedirectToPage("CreateRaceSkillsAndTalents");
        }
    }
}
