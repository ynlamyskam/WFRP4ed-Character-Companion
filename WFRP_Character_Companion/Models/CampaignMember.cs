namespace WFRP_Character_Companion.Models
{
    public class CampaignMember
    {
        public int CampaignId { get; set; }

        public Campaign Campaign { get; set; } = default!;

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = default!;

        public CampaignRole Role { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
