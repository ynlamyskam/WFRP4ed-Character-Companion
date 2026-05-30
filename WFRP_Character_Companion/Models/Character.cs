using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WFRP_Character_Companion.Models
{
    public class Character
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = default!;

        public int WeaponSkillBasic { get; set; } = 0;
        public int BallisticSkillBasic { get; set; } = 0;
        public int StrengthBasic { get; set; } = 0;
        public int ToughnessBasic { get; set; } = 0;
        public int InitiativeBasic { get; set; } = 0;
        public int AgilityBasic { get; set; } = 0;
        public int DexterityBasic { get; set; } = 0;
        public int IntelligenceBasic { get; set; } = 0;
        public int WillpowerBasic { get; set; } = 0;
        public int FellowshipBasic { get; set; } = 0;
    }
}
