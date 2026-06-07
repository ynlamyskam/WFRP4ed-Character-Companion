using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Models.ViewModels;
using WFRP_Character_Companion.Services;

namespace WFRP_Character_Companion.Pages.Campaigns
{
    [Authorize]
    public class AddCharacterModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        public int CampaignId { get; set; }

        public string CampaignName { get; set; } = string.Empty;

        public CampaignListItemVm CampaignView { get; set; } = new();

        public bool CanManage { get; set; }

        public List<Character> UserCharacters { get; set; } = [];

        [BindProperty]
        public List<int> SelectedCharacterIds { get; set; } = [];

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (!await CampaignQueries.IsMemberAsync(_db, user.Id, id))
                return NotFound();

            if (!await LoadPageAsync(id, user.Id))
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (!await CampaignQueries.IsMemberAsync(_db, user.Id, id))
                return NotFound();

            var userCharacterIds = await _db.Characters
                .Where(c => c.UserId == user.Id)
                .Select(c => c.Id)
                .ToListAsync();

            var selected = SelectedCharacterIds
                .Where(userCharacterIds.Contains)
                .ToHashSet();

            var linked = await _db.CampaignCharacters
                .Where(cc => cc.CampaignId == id && userCharacterIds.Contains(cc.CharacterId))
                .ToListAsync();

            var toRemove = linked.Where(cc => !selected.Contains(cc.CharacterId)).ToList();
            if (toRemove.Count > 0)
                _db.CampaignCharacters.RemoveRange(toRemove);

            var linkedIds = linked.Select(cc => cc.CharacterId).ToHashSet();
            foreach (var characterId in selected.Where(cid => !linkedIds.Contains(cid)))
            {
                _db.CampaignCharacters.Add(new CampaignCharacter
                {
                    CampaignId = id,
                    CharacterId = characterId
                });
            }

            await _db.SaveChangesAsync();

            return RedirectToPage(new { id });
        }

        private async Task<bool> LoadPageAsync(int id, string userId)
        {
            var campaign = await _db.Campaigns
                .Include(c => c.Members)
                    .ThenInclude(m => m.User)
                .Include(c => c.CampaignCharacters)
                    .ThenInclude(cc => cc.Character)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
                return false;

            CampaignId = campaign.Id;
            CampaignName = campaign.Name;
            CampaignView = CampaignQueries.ToListItem(campaign, userId);
            CanManage = CampaignView.CanManage;

            var linkedIds = campaign.CampaignCharacters
                .Where(cc => cc.Character.UserId == userId)
                .Select(cc => cc.CharacterId)
                .ToHashSet();

            UserCharacters = await _db.Characters
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            SelectedCharacterIds = UserCharacters
                .Where(c => linkedIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToList();

            return true;
        }
    }
}
