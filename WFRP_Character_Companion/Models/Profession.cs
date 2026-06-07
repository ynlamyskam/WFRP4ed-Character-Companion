namespace WFRP_Character_Companion.Models
{
    public class Profession
    {
        public string? Class { get; set; }
        public string? Name { get; set; }
        public List<ProfessionTier> Tiers { get; set; } = new();
    }

    public class ProfessionTier
    {
        public string Name { get; set; }
        public string? Status { get; set; }
        public List<string> Attributes { get; set; } = new();
        public List<string> Skills { get; set; } = new();
        public List<string> Talents { get; set; } = new();
    }
}
