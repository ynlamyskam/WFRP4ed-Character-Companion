using Microsoft.EntityFrameworkCore;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Models.ViewModels;

namespace WFRP_Character_Companion.Services
{
    public static class CampaignQueries
    {
        public static Task<bool> IsMemberAsync(ApplicationDbContext db, string userId, int campaignId) =>
            db.CampaignMembers.AnyAsync(m => m.CampaignId == campaignId && m.UserId == userId);

        public static Task<bool> IsGameMasterAsync(ApplicationDbContext db, string userId, int campaignId) =>
            db.CampaignMembers.AnyAsync(m =>
                m.CampaignId == campaignId
                && m.UserId == userId
                && m.Role == CampaignRole.GameMaster);

        public static async Task<List<CampaignListItemVm>> LoadUserCampaignsAsync(
            ApplicationDbContext db,
            string userId)
        {
            var campaigns = await db.Campaigns
                .Where(c => c.Members.Any(m => m.UserId == userId))
                .Include(c => c.Members)
                    .ThenInclude(m => m.User)
                .Include(c => c.CampaignCharacters)
                    .ThenInclude(cc => cc.Character)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return campaigns.Select(c => ToListItem(c, userId)).ToList();
        }

        public static CampaignListItemVm ToListItem(Campaign campaign, string currentUserId)
        {
            var charactersByUser = campaign.CampaignCharacters
                .GroupBy(cc => cc.Character.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return new CampaignListItemVm
            {
                Id = campaign.Id,
                Name = campaign.Name,
                IsOwner = campaign.OwnerUserId == currentUserId,
                CanManage = campaign.Members.Any(m =>
                    m.UserId == currentUserId && m.Role == CampaignRole.GameMaster),
                Members = campaign.Members
                    .OrderBy(m => m.Role)
                    .ThenBy(m => m.User.DisplayName)
                    .Select(m => new CampaignMemberVm
                    {
                        UserId = m.UserId,
                        DisplayName = string.IsNullOrWhiteSpace(m.User.DisplayName)
                            ? m.User.Email ?? m.User.UserName ?? "Użytkownik"
                            : m.User.DisplayName,
                        Characters = charactersByUser.TryGetValue(m.UserId, out var chars)
                            ? chars.Select(cc => new CampaignCharacterVm
                            {
                                CharacterId = cc.CharacterId,
                                Name = cc.Character.Name
                            }).OrderBy(x => x.Name).ToList()
                            : []
                    })
                    .ToList()
            };
        }
    }
}
