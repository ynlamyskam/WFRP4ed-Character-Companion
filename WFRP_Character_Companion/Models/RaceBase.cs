namespace WFRP_Character_Companion.Models
{
    public class RaceBase
    {
        public string? Name { get; set; }
        public Dictionary<string, int> Bases { get; set; } = new();
        public int FateBase { get; set; }
        public int FateToAssign { get; set; }
        public int HeroBase { get; set; }
        public int HeroToAssign { get; set; }
    }
}
