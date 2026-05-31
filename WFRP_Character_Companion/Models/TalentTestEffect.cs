namespace WFRP_Character_Companion.Models
{
    public class TalentTestEffect
    {
        public int TalentId { get; set; }
        public Talent Talent { get; set; } = default!;

        public string SkillName { get; set; } = string.Empty;
        public string? Condition { get; set; }
        public int BonusPerLevelAbove1 { get; set; } = 1;
    }
}
