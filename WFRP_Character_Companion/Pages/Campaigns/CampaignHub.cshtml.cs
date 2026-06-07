using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Models.ViewModels;
using WFRP_Character_Companion.Services;

namespace WFRP_Character_Companion.Pages.Campaigns
{
    [Authorize]
    public class CampaignHubModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : PageModel
    {
        private readonly ApplicationDbContext _db = db;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        public List<CampaignListItemVm> Campaigns { get; set; } = [];

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            Campaigns = await CampaignQueries.LoadUserCampaignsAsync(_db, user.Id);
        }
    }
}
