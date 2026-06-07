namespace WFRP_Character_Companion.Models
{
    public class CampaignCharacter
    {
        public int CampaignId { get; set; }

        public Campaign Campaign { get; set; } = default!;

        public int CharacterId { get; set; }

        public Character Character { get; set; } = default!;

        public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    }
}
