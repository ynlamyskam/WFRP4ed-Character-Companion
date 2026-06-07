using System.ComponentModel.DataAnnotations;

namespace WFRP_Character_Companion.Models
{
    public class Campaign
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string OwnerUserId { get; set; } = string.Empty;

        public ApplicationUser Owner { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CampaignMember> Members { get; set; } = [];

        public ICollection<CampaignCharacter> CampaignCharacters { get; set; } = [];
    }
}
