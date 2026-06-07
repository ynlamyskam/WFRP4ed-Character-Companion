using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Models.ViewModels;

namespace WFRP_Character_Companion.Pages.Campaigns
{
    [Authorize]
    public class CreateModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        [BindProperty]
        public CreateCampaignInput Input { get; set; } = new();

        public List<Character> UserCharacters { get; set; } = [];

        public async Task OnGetAsync()
        {
            var user = await RequireUserAsync();
            if (user == null) return;

            UserCharacters = await _db.Characters
                .Where(c => c.UserId == user.Id)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await RequireUserAsync();
            if (user == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                ModelState.AddModelError(nameof(Input.Name), "Nazwa kampanii jest wymagana.");
                await OnGetAsync();
                return Page();
            }

            var campaign = new Campaign
            {
                Name = Input.Name.Trim(),
                OwnerUserId = user.Id,
                Members =
                [
                    new CampaignMember
                    {
                        UserId = user.Id,
                        Role = CampaignRole.GameMaster
                    }
                ]
            };

            var ownedCharacterIds = await _db.Characters
                .Where(c => c.UserId == user.Id && Input.CharacterIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var characterId in ownedCharacterIds)
            {
                campaign.CampaignCharacters.Add(new CampaignCharacter { CharacterId = characterId });
            }

            if (!string.IsNullOrWhiteSpace(Input.MemberEmail))
            {
                var invited = await _userManager.FindByEmailAsync(Input.MemberEmail.Trim());
                if (invited == null)
                {
                    ModelState.AddModelError(nameof(Input.MemberEmail), "Nie znaleziono użytkownika o podanym adresie e-mail.");
                    UserCharacters = await _db.Characters
                        .Where(c => c.UserId == user.Id)
                        .OrderBy(c => c.Name)
                        .ToListAsync();
                    return Page();
                }

                if (invited.Id != user.Id)
                {
                    campaign.Members.Add(new CampaignMember
                    {
                        UserId = invited.Id,
                        Role = CampaignRole.Player
                    });
                }
            }

            _db.Campaigns.Add(campaign);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Campaigns/Edit", new { id = campaign.Id });
        }

        private async Task<ApplicationUser?> RequireUserAsync() =>
            await _userManager.GetUserAsync(User);
    }
}
